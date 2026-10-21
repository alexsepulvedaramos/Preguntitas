using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Packs;

// Pack content in each member's language (rama 24): a pack question reads in the language of
// the request, user-created questions stay in their author's words, Spanish is the fallback.
[Collection(PostgresCollection.Name)]
public class ContentTranslationTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    // Today's question is open and carlos was drawn to choose tomorrow's. The pack has one
    // translated poll.
    private async Task<(SeededGroup Group, int TemplateId)> SeedAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["bea"]);
        var autoPick = await AddQuestionAsync(group.Id, text: "Elegida automáticamente", creatorId: group["ana"]);
        await AddEntryAsync(group.Id, Tomorrow, autoPick, group["carlos"], activated: false);
        Clock.SetLocal(Today, new TimeOnly(22, 0));

        await using var db = NewContext();
        var template = new QuestionTemplate
        {
            Key = "base-playa-o-montana",
            Text = "¿Playa o montaña?",
            TextTranslations = new() { ["en"] = "Beach or mountains?", ["de"] = "Strand oder Berge?" },
            Type = QuestionType.CustomPoll,
            Pack = new Pack
            {
                Name = "Base",
                Description = "Preguntas base disponibles para todos los grupos.",
                NameTranslations = new() { ["en"] = "Basics" },
                DescriptionTranslations = new() { ["en"] = "Basic questions for every group." },
                IsActiveByDefault = true,
            },
            Options =
            [
                new() { Text = "Playa", TextTranslations = new() { ["en"] = "Beach", ["de"] = "Strand" } },
                new() { Text = "Montaña", TextTranslations = new() { ["en"] = "Mountains", ["de"] = "Berge" } },
            ],
        };
        db.QuestionTemplates.Add(template);
        await db.SaveChangesAsync();
        return (group, template.Id);
    }

    private async Task<SelectResult> SelectAsync(int groupId, int userId, SelectQuestionDto dto)
    {
        await using var db = NewContext();
        return (await NewDailyService(db).SelectQuestionAsync(groupId, userId, dto)).Result;
    }

    // The UI language arrives as Accept-Language and becomes CurrentUICulture. Setting it inside
    // this async method keeps the change local to the call (CurrentUICulture is async-local).
    private async Task<QuestionToVoteDto> PendingQuestionAsync(SeededGroup group, string language)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        await using var db = NewContext();
        var status = await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["carlos"]);
        return status.Selection!.PendingQuestion!;
    }

    [Theory]
    [InlineData("es", "¿Playa o montaña?", "Playa", "Montaña")]
    [InlineData("en", "Beach or mountains?", "Beach", "Mountains")]
    [InlineData("de", "Strand oder Berge?", "Strand", "Berge")]
    [InlineData("fr", "¿Playa o montaña?", "Playa", "Montaña")]
    public async Task APackQuestion_ReadsInTheRequestLanguage(string language, string text, string first, string second)
    {
        var (group, templateId) = await SeedAsync();
        (await SelectAsync(group.Id, group["carlos"], new() { TemplateId = templateId })).Should().Be(SelectResult.Success);

        var question = await PendingQuestionAsync(group, language);

        question.Text.Should().Be(text);
        question.Options.Select(o => o.Text).Should().Equal(first, second);
    }

    [Fact]
    public async Task ResultsOfAPackPoll_NameTheOptionsInTheRequestLanguage()
    {
        var (group, templateId) = await SeedAsync();
        await SelectAsync(group.Id, group["carlos"], new() { TemplateId = templateId });
        await using var db = NewContext();
        var clone = await db.Questions.Include(q => q.Options).SingleAsync(q => q.TemplateId == templateId);
        db.Votes.Add(new Vote { QuestionId = clone.Id, UserId = group["ana"], SelectedOptionId = clone.Options.Min(o => o.Id) });
        await db.SaveChangesAsync();

        var results = await ResultsAsync("de", clone.Id);

        results.Text.Should().Be("Strand oder Berge?");
        results.Results.Select(r => r.DisplayText).Should().BeEquivalentTo("Strand", "Berge");
    }

    private async Task<QuestionResultDto> ResultsAsync(string language, int questionId)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        await using var db = NewContext();
        var question = await db.Questions.Include(q => q.Options).SingleAsync(q => q.Id == questionId);
        var votes = await db.Votes.Include(v => v.User).Where(v => v.QuestionId == questionId).ToListAsync();
        return ResultsBuilder.Build(question, votes, TestMapper.Instance);
    }

    [Fact]
    public async Task EditingAPackQuestionWithoutRewordingIt_KeepsItsTranslations()
    {
        var (group, templateId) = await SeedAsync();
        await SelectAsync(group.Id, group["carlos"], new() { TemplateId = templateId });
        var cloneId = await CloneIdAsync(templateId);

        var result = await SelectAsync(group.Id, group["carlos"], Overwrite(cloneId, "¿Playa o montaña?", "Playa", "Montaña"));

        result.Should().Be(SelectResult.Success);
        var question = await PendingQuestionAsync(group, "en");
        question.Text.Should().Be("Beach or mountains?");
        question.Options.Select(o => o.Text).Should().Equal("Beach", "Mountains");
    }

    [Fact]
    public async Task RewordingAPackQuestion_ShowsTheSelectorsWordsToEveryone()
    {
        var (group, templateId) = await SeedAsync();
        await SelectAsync(group.Id, group["carlos"], new() { TemplateId = templateId });
        var cloneId = await CloneIdAsync(templateId);

        await SelectAsync(group.Id, group["carlos"], Overwrite(cloneId, "¿Playa o montaña?", "Playa", "Piscina"));

        var question = await PendingQuestionAsync(group, "en");
        question.Text.Should().Be("¿Playa o montaña?");
        question.Options.Select(o => o.Text).Should().Equal("Playa", "Piscina");
    }

    [Fact]
    public async Task History_ShowsPackQuestionsInTheRequestLanguage_AndUserQuestionsAsWritten()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var pack = await AddQuestionAsync(
            group.Id,
            text: "¿Quién llegaría tarde a su propia boda?",
            configure: q =>
            {
                q.Source = QuestionSource.Pack;
                q.TextTranslations = new() { ["en"] = "Who would be late to their own wedding?" };
            }
        );
        var own = await AddQuestionAsync(group.Id, text: "¿Quién se comió mi tortilla?", creatorId: group["ana"]);
        await AddEntryAsync(group.Id, Today.AddDays(-2), pack, group["ana"], activated: true);
        await AddEntryAsync(group.Id, Today.AddDays(-1), own, group["bea"], activated: true);
        await AddActivatedDayAsync(group.Id, Today, group["ana"]);

        var history = await HistoryAsync("en", group.Id);

        history.Should().Equal("¿Quién se comió mi tortilla?", "Who would be late to their own wedding?");
    }

    private async Task<List<string>> HistoryAsync(string language, int groupId)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        await using var db = NewContext();
        var page = await new QuestionsService(db, TestMapper.Instance, Clock).GetHistoryAsync(groupId, userId: 0, before: null);
        return page.Items.Select(i => i.QuestionText).ToList();
    }

    [Fact]
    public async Task ThePackPicker_ListsPacksAndQuestionsInTheRequestLanguage()
    {
        var (group, _) = await SeedAsync();

        var (packs, templates) = await PickerAsync("en", group.Id);

        packs.Should().ContainSingle().Which.Should().Be(("Basics", "Basic questions for every group."));
        templates.Should().ContainSingle().Which.Should().Be("Beach or mountains? (Beach / Mountains)");
    }

    private async Task<(List<(string, string)> Packs, List<string> Templates)> PickerAsync(string language, int groupId)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        await using var db = NewContext();
        var service = new PacksService(db);
        var packs = await service.GetPacksForGroupAsync(groupId);
        var templates = await service.GetTemplatesAsync(groupId);
        return (
            packs.Select(p => (p.Name, p.Description)).ToList(),
            templates.Items.Select(t => $"{t.Text} ({string.Join(" / ", t.Options)})").ToList()
        );
    }

    [Fact]
    public async Task StreakChatMessages_ReadInTheReadersLanguage_OldOnesAsWritten()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var (entryId, questionId) = await AddActivatedDayAsync(group.Id, Today, group["ana"]);
        Clock.SetLocal(Today, new TimeOnly(22, 0));
        await using (var db = NewContext())
        {
            // The chat only opens once you've voted (rama 49).
            db.Votes.Add(new Vote { QuestionId = questionId, UserId = group["bea"], SelectedTargetUserId = group["ana"] });
            db.ChatMessages.AddRange(
                new ChatMessage { DailyEntryId = entryId, Body = "🔥 ana lleva 7 días seguidos · ¡Fuente anónima!", StreakUsername = "ana", StreakDays = 7 },
                new ChatMessage { DailyEntryId = entryId, Body = "Mensaje de sistema antiguo", CreatedAt = DateTime.UtcNow.AddSeconds(1) }
            );
            await db.SaveChangesAsync();
        }

        var bodies = await ChatAsync("en", group.Id, group["bea"]);

        bodies.Should().Equal("ana is on a 7-day streak · Anonymous Source!", "Mensaje de sistema antiguo");
    }

    private async Task<List<string>> ChatAsync(string language, int groupId, int userId)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        await using var db = NewContext();
        var messages = await new ChatService(db, Notifications.Object).GetMessagesAsync(groupId, userId);
        return messages!.Messages.Select(m => m.Body).ToList();
    }

    private async Task<int> CloneIdAsync(int templateId)
    {
        await using var db = NewContext();
        return await db.Questions.Where(q => q.TemplateId == templateId).Select(q => q.Id).SingleAsync();
    }

    private static SelectQuestionDto Overwrite(int questionId, string text, params string[] options) =>
        new()
        {
            OverwriteQuestionId = questionId,
            NewQuestion = new CreateQuestionDto
            {
                Text = text,
                Type = QuestionType.CustomPoll,
                MinSelections = 1,
                MaxSelections = 1,
                Options = [.. options.Select(o => new CreateOptionDto { Text = o })],
            },
        };
}
