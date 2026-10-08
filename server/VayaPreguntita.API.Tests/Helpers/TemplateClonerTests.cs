using FluentAssertions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Helpers;

public class TemplateClonerTests
{
    private static readonly int[] Members = [11, 12, 13, 14, 15];

    private static QuestionTemplate CustomPollTemplate(bool allowOther) =>
        new()
        {
            Id = 103,
            Text = "What time shall we have dinner?",
            Type = QuestionType.CustomPoll,
            Metadata = new QuestionMetadata
            {
                MinSelections = 1,
                MaxSelections = 2,
                AllowOther = allowOther,
            },
            Options =
            [
                new QuestionTemplateOption { Text = "20:00" },
                new QuestionTemplateOption { Text = "21:00" },
            ],
        };

    [Fact]
    public void CloneToGroup_CustomPollTemplateWithAllowOther_PreservesAllowOther()
    {
        var template = CustomPollTemplate(allowOther: true);

        var question = TemplateCloner.CloneToGroup(
            template,
            groupId: 1,
            activeMemberIds: [1, 2, 3]
        );

        question.Metadata.AllowOther.Should().BeTrue();
        question.Metadata.MinSelections.Should().Be(1);
        question.Metadata.MaxSelections.Should().Be(2);
        question.Options.Select(o => o.Text).Should().Equal("20:00", "21:00");
    }

    [Fact]
    public void CloneToGroup_CustomPollTemplateWithoutAllowOther_KeepsAllowOtherFalse()
    {
        var template = CustomPollTemplate(allowOther: false);

        var question = TemplateCloner.CloneToGroup(
            template,
            groupId: 1,
            activeMemberIds: [1, 2, 3]
        );

        question.Metadata.AllowOther.Should().BeFalse();
    }

    [Fact]
    public void CloneToGroup_Scale_NeverAssignsATargetMember()
    {
        var template = new QuestionTemplate
        {
            Text = "Del 1 al 10, ¿qué tan buen conductor te consideras?",
            Type = QuestionType.Scale,
            Metadata = new QuestionMetadata { RangeMin = 1, RangeMax = 10 },
        };

        // The target used to be picked at random, so repeat to make a regression unmissable.
        for (var i = 0; i < 50; i++)
        {
            var clone = TemplateCloner.CloneToGroup(template, groupId: 7, Members);

            clone.Metadata.TargetUserId.Should().BeNull();
            clone.Metadata.RangeMin.Should().Be(1);
            clone.Metadata.RangeMax.Should().Be(10);
        }
    }

    [Fact]
    public void CloneToGroup_Deathmatch_SplitsEveryMemberIntoTwoBalancedTeams()
    {
        var template = new QuestionTemplate
        {
            Text = "Quiz de bar: ¿qué equipo gana?",
            Type = QuestionType.Deathmatch,
        };

        var clone = TemplateCloner.CloneToGroup(template, groupId: 7, Members);

        clone.Metadata.Teams.Should().HaveCount(2);
        clone.Metadata.Teams.SelectMany(t => t).Should().BeEquivalentTo(Members);
        clone
            .Metadata.Teams.Select(t => t.Count)
            .Should()
            .BeEquivalentTo(new[] { 3, 2 }, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void CloneToGroup_Poll_MarksTheQuestionAsFromThePack()
    {
        var template = CustomPollTemplate(allowOther: false);

        var clone = TemplateCloner.CloneToGroup(template, groupId: 7, Members);

        clone.Source.Should().Be(QuestionSource.Pack);
        clone.CreatorId.Should().BeNull();
        clone.TemplateId.Should().Be(103);
        clone.GroupId.Should().Be(7);
    }
}
