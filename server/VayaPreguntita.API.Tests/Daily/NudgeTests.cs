using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Nudges;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Toque / nudge (rama 43, §17.10): a push to a member who hasn't voted an open question, only
// when it can reach them (a push device, nudges on, the group not muted) and nobody nudged
// them in this group in the last 24 h. CoinRewards.NudgePrice or one from the inventory.
[Collection(PostgresCollection.Name)]
public class NudgeTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // Today's question is open; ana has coins; bea and carlos have a push device.
    private async Task<SeededGroup> SeedAsync(int coins = 100)
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["carlos"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
        foreach (var name in new[] { "bea", "carlos" })
            db.DevicePushSubscriptions.Add(
                new DevicePushSubscription
                {
                    UserId = group[name],
                    Endpoint = $"https://push.test/{name}",
                    P256dh = "key",
                    Auth = "auth",
                }
            );
        await db.SaveChangesAsync();
        return group;
    }

    private async Task<(NudgeResult Result, SendNudgeResponseDto? Response)> NudgeAsync(
        SeededGroup group,
        string sender,
        string target,
        string? message = null,
        bool useOwned = false
    )
    {
        await using var db = NewContext();
        return await NewNudgeService(db)
            .SendAsync(
                group.Id,
                group[sender],
                new SendNudgeRequestDto { TargetUserId = group[target], Message = message, UseOwned = useOwned }
            );
    }

    private async Task<List<NudgeTargetDto>> TargetsAsync(SeededGroup group, string viewer)
    {
        await using var db = NewContext();
        return (await NewNudgeService(db).GetTargetsAsync(group.Id, group[viewer], null))!;
    }

    [Fact]
    public async Task Nudging_ChargesThePrice_AndPushesTheMessage()
    {
        var group = await SeedAsync();

        var (result, response) = await NudgeAsync(group, "ana", "bea", "  Vótame a mí y te invito a una cerve  ");

        result.Should().Be(NudgeResult.Success);
        response!.Balance.Should().Be(100 - CoinRewards.NudgePrice);
        Notifications.Verify(
            n => n.SendNudgeAsync(
                group.Id,
                group["bea"],
                "Los de siempre",
                "ana",
                "Vótame a mí y te invito a una cerve",
                It.IsAny<TimeSpan>()
            ),
            Times.Once
        );

        await using var db = NewContext();
        var nudge = await db.Nudges.SingleAsync();
        var charge = await db.CoinTransactions.SingleAsync(t => t.Reason == CoinReason.Nudge);
        charge.RefId.Should().Be(nudge.Id);
    }

    [Fact]
    public async Task OnlyOnceEvery24Hours_PerMember_FromAnyone()
    {
        var group = await SeedAsync();
        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["carlos"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 100));

        (await NudgeAsync(group, "ana", "bea")).Result.Should().Be(NudgeResult.Success);
        (await NudgeAsync(group, "carlos", "bea")).Result.Should().Be(NudgeResult.Recent);
        (await TargetsAsync(group, "ana")).Single(t => t.UserId == group["bea"]).Status.Should().Be(NudgeTargetDto.Recent);

        Clock.Advance(TimeSpan.FromHours(23));
        (await NudgeAsync(group, "carlos", "bea")).Result.Should().Be(NudgeResult.Recent);
        Clock.Advance(TimeSpan.FromHours(2));
        (await NudgeAsync(group, "carlos", "bea")).Result.Should().Be(NudgeResult.Success);
    }

    [Fact]
    public async Task NotWhenItCantReachThem()
    {
        var group = await SeedAsync();
        await using (var db = NewContext())
        {
            // bea muted the group; carlos turned nudges off.
            await db.GroupMembers.Where(m => m.GroupId == group.Id && m.UserId == group["bea"])
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.NotificationsMuted, true));
            db.NotificationPreferences.Add(new NotificationPreferences { UserId = group["carlos"], Nudge = false });
            await db.SaveChangesAsync();
        }

        (await NudgeAsync(group, "ana", "bea")).Result.Should().Be(NudgeResult.Unreachable);
        (await NudgeAsync(group, "ana", "carlos")).Result.Should().Be(NudgeResult.Unreachable);
        (await TargetsAsync(group, "ana")).Should().OnlyContain(t => t.Status == NudgeTargetDto.Unreachable);

        await using var check = NewContext();
        (await check.Nudges.AnyAsync()).Should().BeFalse();
        (await check.Users.SingleAsync(u => u.Id == group["ana"])).Coins.Should().Be(100);
    }

    [Fact]
    public async Task NotWithoutAPushDevice()
    {
        var group = await SeedAsync();
        await using (var db = NewContext())
            await db.DevicePushSubscriptions.Where(s => s.UserId == group["bea"]).ExecuteDeleteAsync();

        (await NudgeAsync(group, "ana", "bea")).Result.Should().Be(NudgeResult.Unreachable);
    }

    [Fact]
    public async Task NotToSomeoneWhoVoted_OrToYourself()
    {
        var group = await SeedAsync();
        await using (var db = NewContext())
            await NewDailyService(db).VoteAsync(group.Id, group["bea"], new() { SelectedTargetUserId = group["ana"] });

        (await NudgeAsync(group, "ana", "bea")).Result.Should().Be(NudgeResult.AlreadyVoted);
        (await NudgeAsync(group, "ana", "ana")).Result.Should().Be(NudgeResult.InvalidTarget);
        (await TargetsAsync(group, "ana")).Select(t => t.UserId).Should().Equal(group["carlos"]);
    }

    [Fact]
    public async Task ALongMessage_OrNotEnoughCoins_ChangesNothing()
    {
        var group = await SeedAsync(coins: 5);

        (await NudgeAsync(group, "ana", "bea", new string('a', NudgeRules.MaxMessageLength + 1)))
            .Result.Should().Be(NudgeResult.MessageTooLong);
        (await NudgeAsync(group, "ana", "bea")).Result.Should().Be(NudgeResult.NotEnoughCoins);

        await using var db = NewContext();
        (await db.Nudges.AnyAsync()).Should().BeFalse();
        Notifications.Verify(
            n => n.SendNudgeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AnOwnedNudge_IsUsedInsteadOfCoins()
    {
        var group = await SeedAsync();
        await using (var db = NewContext())
            await NewInventoryService(db).BuyAsync(group["ana"], InventoryItem.Nudge);

        var (result, response) = await NudgeAsync(group, "ana", "bea", useOwned: true);

        result.Should().Be(NudgeResult.Success);
        response!.Balance.Should().Be(100 - CoinRewards.NudgePrice);
        response.Inventory.Nudge.Should().Be(0);
    }
}
