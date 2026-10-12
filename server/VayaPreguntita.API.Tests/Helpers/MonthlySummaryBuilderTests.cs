using FluentAssertions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using static VayaPreguntita.API.Helpers.MonthlySummaryBuilder;

namespace VayaPreguntita.API.Tests.Helpers;

// The rules of the monthly summary (§17.12), on hand-built data.
public class MonthlySummaryBuilderTests
{
    private const int Ana = 1, Bea = 2, Carlos = 3, Dani = 4;
    private static readonly DateTime Joined = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly MemberData[] FourMembers =
        [new(Ana, Joined), new(Bea, Joined), new(Carlos, Joined), new(Dani, Joined)];

    // Day n of October, question id 100 + n, entry id n.
    private static EntryData Day(
        int day,
        QuestionType type = QuestionType.Superlative,
        int? author = null,
        int selector = Ana,
        SelectionKind kind = SelectionKind.Auto,
        string? text = null) =>
        new(day, new DateOnly(2026, 10, day), new DateTime(2026, 10, day, 19, 0, 0, DateTimeKind.Utc),
            100 + day, type, text ?? $"Pregunta {day}", author, selector, kind);

    private static VoteData Vote(int day, int user, int? target = null) => new(100 + day, user, target);

    private static MonthlySummaryPayload Build(
        EntryData[] entries,
        VoteData[] votes,
        MemberData[]? members = null,
        (int EntryId, int UserId)[]? guesses = null) =>
        MonthlySummaryBuilder.Build(entries, members ?? FourMembers, votes, guesses ?? []);

    private static MonthlyAward? Award(MonthlySummaryPayload p, string key) =>
        p.Awards.SingleOrDefault(a => a.Key == key);

    [Fact]
    public void Participation_CountsOnTimeAnswersOverTheQuestionsEachMemberCouldAnswer()
    {
        var p = Build(
            [Day(1), Day(2), Day(3), Day(4)],
            [Vote(1, Ana), Vote(2, Ana), Vote(4, Ana), Vote(1, Bea)]);

        var ana = p.Members.Single(m => m.UserId == Ana);
        ana.Answered.Should().Be(3);
        ana.Eligible.Should().Be(4);
        ana.BestRun.Should().Be(2);
        p.Questions.Should().Be(4);
        p.Answered.Should().Be(4);
        p.Eligible.Should().Be(16);
    }

    [Fact]
    public void ALateJoiner_IsOnlyCountedFromTheFirstQuestionStillOpenWhenTheyJoined()
    {
        // Joined on the 3rd at 10:00 UTC: day 2's question (opened the 2nd at 19:00) was
        // still open, day 1's had closed.
        var lateJoiner = new MemberData(Dani, new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc));
        var p = Build(
            [Day(1), Day(2), Day(3)],
            [Vote(3, Dani)],
            [new(Ana, Joined), lateJoiner]);

        var dani = p.Members.Single(m => m.UserId == Dani);
        dani.Eligible.Should().Be(2);
        dani.Answered.Should().Be(1);
    }

    [Fact]
    public void TiedMembers_ShareAnAward_AndNobodyWinsWithZero()
    {
        var p = Build(
            [Day(1), Day(2)],
            [Vote(1, Ana, Bea), Vote(1, Bea, Carlos), Vote(2, Carlos, Bea), Vote(2, Dani, Carlos)]);

        var award = Award(p, AwardKeys.Superlative)!;
        award.Value.Should().Be(2);
        award.Winners.Select(w => w.UserId).Should().Equal(Bea, Carlos);

        Award(p, AwardKeys.Selector).Should().BeNull("every question was auto-selected");
        Award(p, AwardKeys.Guesser).Should().BeNull();
    }

    [Fact]
    public void MostActiveSelector_OnlyCountsQuestionsTheyActuallyChose()
    {
        var p = Build(
            [
                Day(1, selector: Ana, kind: SelectionKind.Picked),
                Day(2, selector: Bea, kind: SelectionKind.Created),
                Day(3, selector: Bea, kind: SelectionKind.Confirmed),
                Day(4, selector: Carlos, kind: SelectionKind.Auto),
            ],
            []);

        var award = Award(p, AwardKeys.Selector)!;
        award.Winners.Should().ContainSingle().Which.UserId.Should().Be(Bea);
        award.Value.Should().Be(2);
    }

    [Fact]
    public void MostConsistent_NeedsAMinimumOfQuestions()
    {
        var days = Enumerable.Range(1, MinEligibleForConsistency).Select(d => Day(d)).ToArray();
        // Dani joined late and voted the only question left: 100 %, but too few to count.
        var lateJoiner = new MemberData(Dani, new DateTime(2026, 10, MinEligibleForConsistency, 10, 0, 0, DateTimeKind.Utc));
        var votes = days.Select(d => Vote(d.EntryId, Ana)).Skip(1)
            .Append(Vote(MinEligibleForConsistency, Dani))
            .ToArray();

        var p = Build(days, votes, [new(Ana, Joined), new(Bea, Joined), lateJoiner]);

        var award = Award(p, AwardKeys.Consistent)!;
        award.Winners.Should().ContainSingle().Which.UserId.Should().Be(Ana);
        award.Value.Should().Be(80);
    }

    [Fact]
    public void StarAuthor_IsTheMemberQuestionWithTheMostAnswers_TiesGoToWhoHadMorePlayed()
    {
        var p = Build(
            [
                Day(1, author: Bea, text: "La de Bea"),
                Day(2, author: Carlos, text: "La buena de Carlos"),
                Day(3, author: Carlos, text: "Otra de Carlos"),
                Day(4, author: null), // a pack question never counts
            ],
            [
                Vote(1, Ana), Vote(1, Carlos),
                Vote(2, Ana), Vote(2, Bea),
                Vote(3, Ana),
                Vote(4, Ana), Vote(4, Bea), Vote(4, Carlos), Vote(4, Dani),
            ]);

        var award = Award(p, AwardKeys.StarAuthor)!;
        award.Value.Should().Be(2);
        award.Winners.Should().ContainSingle().Which.Should().Be(new AwardWinner(Carlos, "La buena de Carlos"));
    }

    [Fact]
    public void BestAuthorGuesser_CountsCorrectGuesses()
    {
        var p = Build([Day(1, author: Bea), Day(2, author: Bea)], [],
            guesses: [(1, Ana), (2, Ana), (1, Carlos)]);

        var award = Award(p, AwardKeys.Guesser)!;
        award.Winners.Should().ContainSingle().Which.UserId.Should().Be(Ana);
        award.Value.Should().Be(2);
    }

    [Fact]
    public void TuMatch_NeedsPicksBothWays_AcrossSuperlativesAndSecretPairings()
    {
        var p = Build(
            [Day(1), Day(2, QuestionType.SecretPairing), Day(3)],
            [
                Vote(1, Ana, Bea), Vote(1, Bea, Ana),          // Ana ⇄ Bea
                Vote(2, Ana, Bea), Vote(2, Ana, Carlos),       // pairing picks
                Vote(3, Carlos, Ana),                          // Carlos → Ana, so Ana ⇄ Carlos
                Vote(3, Dani, Ana),                            // Dani → Ana, never back
                Vote(3, Bea, Bea),                             // a self-pick never counts
            ]);

        p.Matches.Single(m => m.UserId == Ana).Should().BeEquivalentTo(new MonthlyMatch(Ana, [Bea], 3));
        p.Matches.Single(m => m.UserId == Carlos).MatchUserIds.Should().Equal(Ana);
        p.Matches.Should().NotContain(m => m.UserId == Dani);
    }

    [Fact]
    public void FormerMembers_AreLeftOut()
    {
        var p = Build(
            [Day(1)],
            [Vote(1, Ana, Dani), Vote(1, Bea, Dani), Vote(1, Dani, Ana), Vote(1, Carlos, Bea)],
            [new(Ana, Joined), new(Bea, Joined), new(Carlos, Joined)]);

        p.Members.Select(m => m.UserId).Should().Equal(Ana, Bea, Carlos);
        Award(p, AwardKeys.Superlative)!.Winners.Should().ContainSingle().Which.UserId.Should().Be(Bea);
        p.Matches.Should().BeEmpty();
    }
}
