using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Coins;

// Shop cosmetics (rama 42, §17.10; rama 57): bought once with coins, kept forever, equipped
// through the profile only when owned, and shown to the rest of the group through the member
// list. Seasonal frames are only sold in their window and announced once by push.
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

    private static readonly DateOnly HalloweenDay = new(2026, 10, 27);

    [Fact]
    public async Task Buying_ChargesThePrice_OnceAndForAll()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);

        (await BuyAsync(group["ana"], "ring-volt")).Should().Be(CosmeticPurchaseResult.Success);
        (await BuyAsync(group["ana"], "ring-volt")).Should().Be(CosmeticPurchaseResult.AlreadyOwned);

        (await CoinsAsync(group["ana"])).Should().Be(600);
        await using var db = NewContext();
        var row = await db.CoinTransactions.SingleAsync(t => t.UserId == group["ana"]);
        row.Reason.Should().Be(CoinReason.Cosmetic);
        row.Amount.Should().Be(-400);
        row.RefId.Should().Be(ShopCatalog.ByKey("ring-volt")!.Id);
        (await NewCoinService(db).GetHistoryAsync(group["ana"], null)).Items.Single().ItemKey.Should().Be("ring-volt");
    }

    [Fact]
    public async Task BuyingWithoutEnoughCoins_OrAnUnknownItem_ChangesNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 399);

        (await BuyAsync(group["ana"], "ring-volt")).Should().Be(CosmeticPurchaseResult.NotEnoughCoins);
        (await BuyAsync(group["ana"], "ring-gold-plated")).Should().Be(CosmeticPurchaseResult.UnknownItem);

        (await CoinsAsync(group["ana"])).Should().Be(399);
        await using var db = NewContext();
        (await db.UserItems.AnyAsync()).Should().BeFalse();
        (await db.CoinTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task WearingAFinish_RequiresOwningIt_AndPutsTheFrameOnTheStreakRing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await BuyAsync(group["ana"], "ring-volt");
        await BuyAsync(group["ana"], "frame-blue");
        (await UpdateAsync(group["ana"], new() { FrameColor = "blue" })).Success.Should().BeTrue();

        (await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-arcade" }))
            .Error.Should().Be("COSMETIC_NOT_OWNED");
        (await UpdateAsync(group["ana"], new() { RingSkinKey = "frame-blue" }))
            .Error.Should().Be("COSMETIC_NOT_OWNED");
        (await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-volt" })).Success.Should().BeTrue();

        await using (var db = NewContext())
        {
            var ana = await db.Users.SingleAsync(u => u.Id == group["ana"]);
            ana.RingSkinKey.Should().Be("ring-volt");
            ana.FrameColor.Should().Be(FrameColors.Streak);
        }

        (await UpdateAsync(group["ana"], new() { RingSkinKey = "" })).Success.Should().BeTrue();
        await using (var db = NewContext())
            (await db.Users.SingleAsync(u => u.Id == group["ana"])).RingSkinKey.Should().BeNull();
    }

    [Fact]
    public async Task FrameColours_AreBoughtInTheShop_AndTakeTheFinishOff()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await BuyAsync(group["ana"], "ring-volt");
        await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-volt" });

        (await UpdateAsync(group["ana"], new() { FrameColor = "purple" })).Error.Should().Be("COSMETIC_NOT_OWNED");
        (await BuyAsync(group["ana"], "frame-purple")).Should().Be(CosmeticPurchaseResult.Success);
        (await CoinsAsync(group["ana"])).Should().Be(400);
        (await UpdateAsync(group["ana"], new() { FrameColor = "purple" })).Success.Should().BeTrue();

        await using var db = NewContext();
        var ana = await db.Users.SingleAsync(u => u.Id == group["ana"]);
        ana.FrameColor.Should().Be("purple");
        ana.RingSkinKey.Should().BeNull();
    }

    [Fact]
    public async Task FrameColours_TakeALegacyHex_AndTheFreeFramesNeedNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["bea"], 1000);
        await BuyAsync(group["bea"], "frame-blue");

        (await UpdateAsync(group["bea"], new() { FrameColor = "#3B82F6" })).Success.Should().BeTrue();
        (await UpdateAsync(group["ana"], new() { FrameColor = "#123456" })).Error.Should().Be("INVALID_FRAME_COLOR");
        (await UpdateAsync(group["ana"], new() { FrameColor = FrameColors.None })).Success.Should().BeTrue();
        (await UpdateAsync(group["ana"], new() { FrameColor = FrameColors.Streak })).Success.Should().BeTrue();

        await using var db = NewContext();
        (await NewUserService(db).GetProfileAsync(group["bea"]))!.FrameColor.Should().Be("blue");
        (await NewUserService(db).GetProfileAsync(group["ana"]))!.FrameColor.Should().Be(FrameColors.Streak);
    }

    [Fact]
    public async Task TheRetiredSeniorityFrame_FromAnOldClient_BecomesTheStreakRing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.FrameColor, FrameColors.None));

        (await UpdateAsync(group["ana"], new() { FrameColor = FrameColors.RetiredSeniority })).Success.Should().BeTrue();

        await using var check = NewContext();
        (await NewUserService(check).GetProfileAsync(group["ana"]))!.FrameColor.Should().Be(FrameColors.Streak);
    }

    [Fact]
    public async Task RetiredCosmetics_CanNoLongerBeBoughtOrWorn()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);

        foreach (var key in new[] { "badge-frog", "acc-halo", "title-oracle", "color-coral", "theme-ocean" })
            (await BuyAsync(group["ana"], key)).Should().Be(CosmeticPurchaseResult.UnknownItem);
        (await UpdateAsync(group["ana"], new() { TitleKey = "title-oracle" })).Error.Should().Be("TITLE_NOT_UNLOCKED");
        (await CoinsAsync(group["ana"])).Should().Be(1000);
    }

    [Fact]
    public async Task TheLedger_StillNamesARetiredItem()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await using var db = NewContext();
        db.CoinTransactions.Add(
            new()
            {
                UserId = group["ana"],
                Amount = 250,
                Reason = CoinReason.CosmeticRefund,
                RefId = ShopCatalog.Retired.Single(i => i.Key == "theme-ocean").Id,
                CreatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();

        (await NewCoinService(db).GetHistoryAsync(group["ana"], null)).Items.Single().ItemKey.Should().Be("theme-ocean");
    }

    [Fact]
    public async Task TheMemberList_CarriesEachMembersFinish()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await BuyAsync(group["ana"], "ring-aurora");
        await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-aurora" });

        await using var db = NewContext();
        var members = (await NewGroupsService(db).GetGroupMembersAsync(group.Id, group["bea"])).ToList();

        members.Single(m => m.Id == group["ana"]).Look!.RingSkin.Should().Be("ring-aurora");
        members.Single(m => m.Id == group["bea"]).Look.Should().BeNull();
    }

    [Fact]
    public async Task APlate_IsBoughtAndWornApartFromTheRing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 3000);

        // Owning the ring doesn't give its plate: it is a separate item at the ring's price.
        await BuyAsync(group["ana"], "ring-neon");
        (await UpdateAsync(group["ana"], new() { PlateKey = "plate-neon" })).Error.Should().Be("COSMETIC_NOT_OWNED");
        (await BuyAsync(group["ana"], "plate-neon")).Should().Be(CosmeticPurchaseResult.Success);
        (await CoinsAsync(group["ana"])).Should().Be(0);

        // Any plate goes with any ring, and changing the frame leaves the plate alone.
        await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-neon" });
        (await UpdateAsync(group["ana"], new() { PlateKey = "plate-neon" })).Success.Should().BeTrue();
        await UpdateAsync(group["ana"], new() { FrameColor = FrameColors.None });

        await using (var db = NewContext())
        {
            var members = (await NewGroupsService(db).GetGroupMembersAsync(group.Id, group["bea"])).ToList();
            var look = members.Single(m => m.Id == group["ana"]).Look!;
            look.Plate.Should().Be("plate-neon");
            look.RingSkin.Should().BeNull();
        }

        (await UpdateAsync(group["ana"], new() { PlateKey = "" })).Success.Should().BeTrue();
        await using (var db = NewContext())
            (await db.Users.SingleAsync(u => u.Id == group["ana"])).PlateKey.Should().BeNull();
    }

    [Fact]
    public async Task ASeasonalFrame_IsOnlySoldInItsWindow_ButIsWornForever()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 1000);
        await SetCoinsAsync(group["bea"], 1000);

        // Not listed until the week before; then shown as coming soon, not for sale yet.
        Clock.SetLocal(HalloweenDay.AddDays(-8), new TimeOnly(12, 0));
        await using (var db = NewContext())
            (await NewCosmeticsService(db).GetCatalogAsync(group["ana"])).Cosmetics.Should().NotContain(c => c.Key == "ring-witch");
        Clock.SetLocal(HalloweenDay, new TimeOnly(15, 59));
        (await BuyAsync(group["ana"], "ring-witch")).Should().Be(CosmeticPurchaseResult.NotOnSale);
        await using (var db = NewContext())
        {
            var soon = (await NewCosmeticsService(db).GetCatalogAsync(group["ana"])).Cosmetics.Single(c => c.Key == "ring-witch");
            soon.OnSale.Should().BeFalse();
            soon.OnSaleFrom.Should().Be(ShopCatalog.HalloweenFrom);
        }

        Clock.SetLocal(HalloweenDay, new TimeOnly(16, 0));
        (await BuyAsync(group["ana"], "ring-witch")).Should().Be(CosmeticPurchaseResult.Success);
        (await CoinsAsync(group["ana"])).Should().Be(900);
        await using (var db = NewContext())
        {
            var witch = (await NewCosmeticsService(db).GetCatalogAsync(group["bea"])).Cosmetics.Single(c => c.Key == "ring-witch");
            witch.OnSale.Should().BeTrue();
            witch.OnSaleUntil.Should().Be(ShopCatalog.HalloweenUntil);
        }

        Clock.SetLocal(HalloweenDay.AddDays(7), new TimeOnly(16, 0));
        (await BuyAsync(group["bea"], "ring-witch")).Should().Be(CosmeticPurchaseResult.NotOnSale);
        (await UpdateAsync(group["ana"], new() { RingSkinKey = "ring-witch" })).Success.Should().BeTrue();
        await using (var db = NewContext())
        {
            var service = NewCosmeticsService(db);
            (await service.GetCatalogAsync(group["bea"])).Cosmetics.Should().NotContain(c => c.Key == "ring-witch");
            var owned = (await service.GetCatalogAsync(group["ana"])).Cosmetics.Single(c => c.Key == "ring-witch");
            owned.Owned.Should().BeTrue();
            owned.OnSale.Should().BeFalse();
        }
    }

    [Fact]
    public async Task ASeasonalFrame_IsAnnouncedOnce_AtFourInTheAfternoon()
    {
        await SeedGroupAsync("ana", "bea");

        Clock.SetLocal(HalloweenDay, new TimeOnly(15, 59));
        await using (var db = NewContext())
            await NewSeasonalAnnouncementService(db).AnnounceDueAsync();
        Notifications.Verify(n => n.SendSeasonalItemAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);

        Clock.SetLocal(HalloweenDay, new TimeOnly(16, 1));
        await using (var db = NewContext())
            await NewSeasonalAnnouncementService(db).AnnounceDueAsync();
        Clock.SetLocal(HalloweenDay, new TimeOnly(16, 2));
        await using (var db = NewContext())
            await NewSeasonalAnnouncementService(db).AnnounceDueAsync();

        Notifications.Verify(n => n.SendSeasonalItemAsync("ring-witch", 100, 7), Times.Once);
        // The seasonal plate shares the ring's window but gets no push of its own.
        Notifications.Verify(n => n.SendSeasonalItemAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task AMissedAnnouncement_WaitsForTheNextAfternoon()
    {
        await SeedGroupAsync("ana", "bea");

        Clock.SetLocal(HalloweenDay.AddDays(1), new TimeOnly(9, 0));
        await using (var db = NewContext())
            await NewSeasonalAnnouncementService(db).AnnounceDueAsync();
        Notifications.Verify(n => n.SendSeasonalItemAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);

        Clock.SetLocal(HalloweenDay.AddDays(1), new TimeOnly(16, 30));
        await using (var db = NewContext())
            await NewSeasonalAnnouncementService(db).AnnounceDueAsync();
        Notifications.Verify(n => n.SendSeasonalItemAsync("ring-witch", 100, 6), Times.Once);
    }

    [Fact]
    public void TheCatalog_HasUniqueIdsAndKeys_RetiredOnesIncluded()
    {
        var everything = ShopCatalog.All.Concat(ShopCatalog.Retired).ToList();
        everything.Select(i => i.Id).Should().OnlyHaveUniqueItems();
        everything.Select(i => i.Key).Should().OnlyHaveUniqueItems();
        ShopCatalog.All.Should().OnlyContain(i => i.Key.Length <= 40 && i.Price > 0);
        FrameColors.PaletteKeys.Should().OnlyContain(k => ShopCatalog.ByKey(ShopCatalog.FrameColorKey(k)) != null);
    }

    // Every ring finish and every frame colour has its plate, at the same price (§17.18).
    [Fact]
    public void EveryRingAndColour_HasItsPlate_AtTheSamePrice()
    {
        var sources = ShopCatalog.All.Where(i => i.Kind is CosmeticKind.RingSkin)
            .Select(i => (Source: i, Plate: ShopCatalog.ByKey(ShopCatalog.PlateKey(i.Key))))
            .Concat(FrameColors.PaletteKeys.Select(k => (Source: ShopCatalog.ByKey(ShopCatalog.FrameColorKey(k))!, Plate: ShopCatalog.ByKey(ShopCatalog.PlateKey(k)))));
        foreach (var (source, plate) in sources)
        {
            plate.Should().NotBeNull(source.Key);
            plate!.Kind.Should().Be(CosmeticKind.Plate);
            plate.Price.Should().Be(source.Price, plate.Key);
            plate.Rarity.Should().Be(source.Rarity, plate.Key);
            plate.OnSaleFrom.Should().Be(source.OnSaleFrom, plate.Key);
            plate.OnSaleUntil.Should().Be(source.OnSaleUntil, plate.Key);
        }
        ShopCatalog.All.Count(i => i.Kind == CosmeticKind.Plate).Should().Be(sources.Count());
    }

    // What a player earns on a typical day in one group on top of the streak bonus: the vote
    // plus, on average, first voter, picking the question and guessing the author (ledger,
    // 2026-10-21 → 26: ~6.6).
    private const double TypicalDailyCoinsInOneGroup = 6.6;

    // The price of a seasonal frame must be payable by playing every day of its window in one
    // group, starting from nothing and a new streak (§17.15 item 8).
    [Fact]
    public void ASeasonalFrame_CostsNoMoreThanItsWindowOfPlay()
    {
        foreach (var item in ShopCatalog.All.Where(i => i.IsSeasonal && !ShopCatalog.PriceRuleExceptions.Contains(i.Key)))
        {
            var days = (int)(item.OnSaleUntil!.Value - item.OnSaleFrom!.Value).TotalDays;
            var earned = Enumerable.Range(1, days).Sum(day => TypicalDailyCoinsInOneGroup + CoinRewards.StreakBonus(day));
            ((double)item.Price).Should().BeLessThanOrEqualTo(earned, item.Key);
        }
    }
}
