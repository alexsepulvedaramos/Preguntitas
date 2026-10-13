using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Coins;

// Shop cosmetics (rama 42, §17.10): bought once with coins, kept forever, equipped through
// the profile only when owned, and shown to the rest of the group through the member list.
[Collection(PostgresCollection.Name)]
public class CosmeticsTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private async Task SetCoinsAsync(int userId, int coins)
    {
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
    }

    private async Task<int> CoinsAsync(int userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Coins).SingleAsync();
    }

    private async Task<CosmeticPurchaseResult> BuyAsync(int userId, string key)
    {
        await using var db = NewContext();
        return await NewCosmeticsService(db).BuyAsync(userId, key);
    }

    private async Task<(bool Success, string? Error)> UpdateAsync(int userId, UpdateProfileRequestDto request)
    {
        await using var db = NewContext();
        return await NewUserService(db).UpdateProfileAsync(userId, request);
    }

    [Fact]
    public async Task Buying_ChargesThePrice_OnceAndForAll()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);

        (await BuyAsync(group["ana"], "ring-neon")).Should().Be(CosmeticPurchaseResult.Success);
        (await BuyAsync(group["ana"], "ring-neon")).Should().Be(CosmeticPurchaseResult.AlreadyOwned);

        (await CoinsAsync(group["ana"])).Should().Be(600);
        await using var db = NewContext();
        var row = await db.CoinTransactions.SingleAsync(t => t.UserId == group["ana"]);
        row.Reason.Should().Be(CoinReason.Cosmetic);
        row.Amount.Should().Be(-400);
        row.RefId.Should().Be(ShopCatalog.ByKey("ring-neon")!.Id);
        (await NewCoinService(db).GetHistoryAsync(group["ana"], null)).Items.Single().ItemKey.Should().Be("ring-neon");
    }

    [Fact]
    public async Task BuyingWithoutEnoughCoins_OrAnUnknownItem_ChangesNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 399);

        (await BuyAsync(group["ana"], "ring-neon")).Should().Be(CosmeticPurchaseResult.NotEnoughCoins);
        (await BuyAsync(group["ana"], "ring-gold-plated")).Should().Be(CosmeticPurchaseResult.UnknownItem);

        (await CoinsAsync(group["ana"])).Should().Be(399);
        await using var db = NewContext();
        (await db.UserItems.AnyAsync()).Should().BeFalse();
        (await db.CoinTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Equipping_RequiresOwnership_AndTheRightSlot()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await BuyAsync(group["ana"], "acc-halo");

        (await UpdateAsync(group["ana"], new() { AccessoryKey = "acc-sunglasses" }))
            .Error.Should().Be("COSMETIC_NOT_OWNED");
        (await UpdateAsync(group["ana"], new() { RingSkinKey = "acc-halo" }))
            .Error.Should().Be("COSMETIC_NOT_OWNED");
        (await UpdateAsync(group["ana"], new() { AccessoryKey = "acc-halo" })).Success.Should().BeTrue();

        await using (var db = NewContext())
            (await db.Users.SingleAsync(u => u.Id == group["ana"])).AccessoryKey.Should().Be("acc-halo");

        (await UpdateAsync(group["ana"], new() { AccessoryKey = "" })).Success.Should().BeTrue();
        await using (var db = NewContext())
            (await db.Users.SingleAsync(u => u.Id == group["ana"])).AccessoryKey.Should().BeNull();
    }

    [Fact]
    public async Task AnOwnedShopTitle_ReplacesTheStreakTitle()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);

        (await UpdateAsync(group["ana"], new() { TitleKey = "title-oracle" })).Error.Should().Be("TITLE_NOT_UNLOCKED");
        await BuyAsync(group["ana"], "title-oracle");
        (await UpdateAsync(group["ana"], new() { TitleKey = "title-oracle" })).Success.Should().BeTrue();

        await using var db = NewContext();
        var profile = await NewUserService(db).GetProfileAsync(group["ana"]);
        profile!.Title.Should().Be("El Oráculo");
        profile.ShopTitles.Should().ContainSingle(t => t.Key == "title-oracle");
        profile.OwnedCosmetics.Should().Equal("title-oracle");
    }

    [Fact]
    public async Task TheMemberList_CarriesEachMembersLook()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await BuyAsync(group["ana"], "ring-pixel");
        await BuyAsync(group["ana"], "badge-frog");
        await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-pixel", BadgeKey = "badge-frog" });

        await using var db = NewContext();
        var members = (await NewGroupsService(db).GetGroupMembersAsync(group.Id, group["bea"])).ToList();

        var ana = members.Single(m => m.Id == group["ana"]);
        ana.Look!.RingSkin.Should().Be("ring-pixel");
        ana.Look.Badge.Should().Be("badge-frog");
        ana.Look.Accessory.Should().BeNull();
        members.Single(m => m.Id == group["bea"]).Look.Should().BeNull();
    }

    [Fact]
    public async Task TheSeniorityFrame_IsAValidFrameChoice()
    {
        var group = await SeedGroupAsync("ana", "bea");

        (await UpdateAsync(group["ana"], new() { FrameColor = FrameColors.Seniority })).Success.Should().BeTrue();

        await using var db = NewContext();
        var profile = await NewUserService(db).GetProfileAsync(group["ana"]);
        profile!.FrameColor.Should().Be(FrameColors.Seniority);
        profile.FirstJoinedAt.Should().NotBeNull();
    }

    [Fact]
    public void TheCatalog_HasUniqueIdsAndKeys_AndEveryTitleIsNamed()
    {
        ShopCatalog.All.Select(i => i.Id).Should().OnlyHaveUniqueItems();
        ShopCatalog.All.Select(i => i.Key).Should().OnlyHaveUniqueItems();
        ShopCatalog.All.Where(i => i.Kind == CosmeticKind.Title).Should().OnlyContain(i => i.Name != null);
        ShopCatalog.All.Should().OnlyContain(i => i.Key.Length <= 40 && i.Price > 0);
    }
}
