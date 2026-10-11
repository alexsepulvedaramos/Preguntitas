using FluentAssertions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Helpers;

// Per-type aggregation of "who voted for what" (§7.4). Pure: votes are built in memory
// the way VoteAsync stores them.
public class ResultsBuilderTests
{
    private static readonly User Ana = new() { Id = 1, Username = "ana" };
    private static readonly User Bea = new() { Id = 2, Username = "bea" };
    private static readonly User Carlos = new() { Id = 3, Username = "carlos" };
    private static readonly User Dani = new() { Id = 4, Username = "dani" };

    private static Vote Vote(User voter, User? target = null, int? option = null, int? value = null, string? text = null) =>
        new()
        {
            UserId = voter.Id,
            User = voter,
            SelectedTargetUserId = target?.Id,
            SelectedTargetUser = target,
            SelectedOptionId = option,
            NumericValue = value,
            FreeText = text,
        };

    private static Question Poll(bool allowOther = false) =>
        new()
        {
            Id = 50,
            Type = QuestionType.CustomPoll,
            Text = "¿Qué cenamos?",
            Metadata = new QuestionMetadata { MaxSelections = 2, AllowOther = allowOther },
            Options =
            [
                new Option { Id = 10, Text = "Pizza" },
                new Option { Id = 11, Text = "Sushi" },
                new Option { Id = 12, Text = "Tacos" },
            ],
        };

    [Fact]
    public void CustomPoll_ListsEveryOptionMostVotedFirst_IncludingOnesNobodyPicked()
    {
        var votes = new List<Vote> { Vote(Ana, option: 11), Vote(Bea, option: 11), Vote(Carlos, option: 10) };

        var result = ResultsBuilder.Build(Poll(), votes, TestMapper.Instance);

        result.Results.Select(r => (r.DisplayText, r.VoteCount, r.Percentage))
            .Should()
            .Equal(("Sushi", 2, 66.7), ("Pizza", 1, 33.3), ("Tacos", 0, 0.0));
        result.Results[0].Voters.Select(v => v.Username).Should().Equal("ana", "bea");
    }

    [Fact]
    public void TotalVotes_CountsPeopleNotRows()
    {
        // Multi-select: ana picks two options, so she has two rows but is one voter.
        var votes = new List<Vote> { Vote(Ana, option: 10), Vote(Ana, option: 11), Vote(Bea, option: 10) };

        var result = ResultsBuilder.Build(Poll(), votes, TestMapper.Instance);

        result.TotalVotes.Should().Be(2);
        result.Results.Single(r => r.DisplayText == "Pizza").Percentage.Should().Be(100);
        result.Results.Single(r => r.DisplayText == "Sushi").Percentage.Should().Be(50);
    }

    [Fact]
    public void CustomPoll_OtherAnswersAreListedAsFreeTextResponses()
    {
        var votes = new List<Vote> { Vote(Ana, option: 10), Vote(Ana, text: "Croquetas") };

        var result = ResultsBuilder.Build(Poll(allowOther: true), votes, TestMapper.Instance);

        result.TotalVotes.Should().Be(1);
        result.FreeTextResponses.Select(r => (r.UserId, r.Username, r.Text))
            .Should()
            .Equal((Ana.Id, "ana", "Croquetas"));
    }

    [Fact]
    public void Superlative_GroupsVotesByTarget_AndShowsNobody()
    {
        var question = new Question { Id = 51, Type = QuestionType.Superlative, Text = "¿Quién…?" };
        var votes = new List<Vote> { Vote(Ana, target: Bea), Vote(Carlos, target: Bea), Vote(Bea) };

        var result = ResultsBuilder.Build(question, votes, TestMapper.Instance);

        result.Results.Select(r => (r.Id, r.DisplayText, r.VoteCount, r.Percentage))
            .Should()
            .Equal((Bea.Id, "bea", 2, 66.7), (0, "Nadie", 1, 33.3));
        result.Results[0].TargetUser!.Username.Should().Be("bea");
        result.Results[0].Voters.Select(v => v.Username).Should().Equal("ana", "carlos");
    }

    [Fact]
    public void SecretPairing_MergesTheSamePairWhicheverOrderItWasPicked()
    {
        var question = new Question { Id = 52, Type = QuestionType.SecretPairing, Text = "¿Quién haría buena pareja?" };
        var votes = new List<Vote>
        {
            Vote(Ana, target: Bea),
            Vote(Ana, target: Carlos),
            Vote(Dani, target: Carlos),
            Vote(Dani, target: Bea),
            Vote(Bea, target: Ana),
            Vote(Bea, target: Dani),
        };

        var result = ResultsBuilder.Build(question, votes, TestMapper.Instance);

        result.TotalVotes.Should().Be(3);
        result.Results.Select(r => (r.DisplayText, r.VoteCount, r.Percentage))
            .Should()
            .Equal(("bea + carlos", 2, 66.7), ("ana + dani", 1, 33.3));
        result.Results[0].Voters.Select(v => v.Username).Should().Equal("ana", "dani");
    }

    [Fact]
    public void Scale_BinsByValueInAscendingOrder_WithTheFixedRange()
    {
        var question = new Question
        {
            Id = 53,
            Type = QuestionType.Scale,
            Text = "Del 1 al 10, ¿cuánto te gusta madrugar?",
            Metadata = new QuestionMetadata { RangeMin = 1, RangeMax = 10 },
        };
        var votes = new List<Vote> { Vote(Ana, value: 7), Vote(Bea, value: 3), Vote(Carlos, value: 7) };

        var result = ResultsBuilder.Build(question, votes, TestMapper.Instance);

        result.Results.Select(r => (r.DisplayText, r.VoteCount)).Should().Equal(("3", 1), ("7", 2));
        (result.RangeMin, result.RangeMax).Should().Be((1, 10));
    }

    [Fact]
    public void Deathmatch_CountsVotesPerTeam_AndNamesItsMembers()
    {
        var question = new Question
        {
            Id = 54,
            Type = QuestionType.Deathmatch,
            Text = "Quiz de bar: ¿qué equipo gana?",
            Metadata = new QuestionMetadata { Teams = [[Ana.Id, Bea.Id], [Carlos.Id, Dani.Id]] },
        };
        // A Deathmatch vote stores the chosen team's first member.
        var votes = new List<Vote> { Vote(Ana, target: Ana), Vote(Bea, target: Carlos), Vote(Dani, target: Carlos) };
        var usersById = new[] { Ana, Bea, Carlos, Dani }.ToDictionary(u => u.Id);

        var result = ResultsBuilder.Build(question, votes, TestMapper.Instance, usersById);

        result.Results.Select(r => (r.Id, r.DisplayText, r.VoteCount))
            .Should()
            .Equal((2, "carlos + dani", 2), (1, "ana + bea", 1));
        result.Results[0].TeamMembers.Select(m => m.Username).Should().Equal("carlos", "dani");
    }

    [Fact]
    public void OpenText_ListsEveryAnswerWithItsAuthorInOrder()
    {
        var question = new Question { Id = 55, Type = QuestionType.OpenText, Text = "Describe al grupo en una palabra" };
        var votes = new List<Vote> { Vote(Bea, text: "Caos"), Vote(Ana, text: "Familia") };

        var result = ResultsBuilder.Build(question, votes, TestMapper.Instance);

        result.Results.Should().BeEmpty();
        result.TotalVotes.Should().Be(2);
        result.FreeTextResponses.Select(r => (r.Username, r.Text))
            .Should()
            .Equal(("bea", "Caos"), ("ana", "Familia"));
    }

    [Fact]
    public void WithNoVotes_TheResultsAreEmptyButKeepTheScaleRange()
    {
        var question = new Question
        {
            Id = 56,
            Type = QuestionType.Scale,
            Text = "Del 1 al 10…",
            Metadata = new QuestionMetadata { RangeMin = 1, RangeMax = 10 },
        };

        var result = ResultsBuilder.Build(question, [], TestMapper.Instance);

        result.TotalVotes.Should().Be(0);
        result.Results.Should().BeEmpty();
        result.FreeTextResponses.Should().BeEmpty();
        (result.RangeMin, result.RangeMax).Should().Be((1, 10));
    }
}
