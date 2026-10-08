using FluentAssertions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Helpers;

public class TemplateClonerTests
{
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

        var question = TemplateCloner.CloneToGroup(template, groupId: 1, activeMemberIds: [1, 2, 3]);

        question.Metadata.AllowOther.Should().BeTrue();
        question.Metadata.MinSelections.Should().Be(1);
        question.Metadata.MaxSelections.Should().Be(2);
        question.Options.Select(o => o.Text).Should().Equal("20:00", "21:00");
    }

    [Fact]
    public void CloneToGroup_CustomPollTemplateWithoutAllowOther_KeepsAllowOtherFalse()
    {
        var template = CustomPollTemplate(allowOther: false);

        var question = TemplateCloner.CloneToGroup(template, groupId: 1, activeMemberIds: [1, 2, 3]);

        question.Metadata.AllowOther.Should().BeFalse();
    }
}
