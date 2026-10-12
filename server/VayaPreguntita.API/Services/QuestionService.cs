using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Services;

public class QuestionsService(AppDbContext context, IMapper mapper) : IQuestionsService
{
    // §11 abuse-protection limits on the pool of unused (IsUsed == false) questions.
    private const int MaxUnusedQuestionsPerUserPerGroup = 20;
    private const int MaxUnusedQuestionsPerGroup = 500;

    // ==========================================
    // GET POOL
    // Devuelve las preguntas disponibles del grupo (no usadas), paginadas (cursor por Id,
    // monótono y ya implica orden de inserción) y opcionalmente filtradas por tipo.
    // ==========================================
    public async Task<QuestionPageDto> GetPoolAsync(
        int groupId,
        int userId,
        QuestionType? type = null,
        int? before = null,
        int pageSize = 12
    )
    {
        var query = context.Questions.Include(q => q.Options).Where(q => q.GroupId == groupId && !q.IsUsed);

        if (type != null)
            query = query.Where(q => q.Type == type);

        if (before != null)
            query = query.Where(q => q.Id < before);

        var questions = await query.OrderByDescending(q => q.Id).Take(pageSize + 1).ToListAsync();
        var items = questions.Select(q => ToDto(q, userId)).ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
            items.RemoveAt(items.Count - 1);

        return new QuestionPageDto { Items = items, HasMore = hasMore };
    }

    // ==========================================
    // GET BY DATE
    // Devuelve la pregunta y resultados de una fecha concreta (historial). Gated (§17.7):
    // without your vote you get the question and the vote count, not the results.
    // ==========================================
    public async Task<QuestionResultDto?> GetByDateAsync(int groupId, int userId, DateOnly date)
    {
        var entry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .Include(d => d.Question)
                .ThenInclude(q => q.Votes)
                    .ThenInclude(v => v.User)
            .Include(d => d.Question)
                .ThenInclude(q => q.Votes)
                    .ThenInclude(v => v.SelectedTargetUser)
            // Only activated entries: tomorrow's preselected question stays a surprise.
            .FirstOrDefaultAsync(d => d.GroupId == groupId && d.Date == date && d.ActivatedAt != null);

        if (entry == null)
            return null;

        var usersById = await ResultsBuilder.BuildDeathmatchUsersById(context, entry.Question);
        var result = ResultsBuilder.Build(entry.Question, entry.Question.Votes, mapper, usersById);

        // The author shows only once the question has closed (a newer one activated): while
        // it's open, members are still guessing (§17.6).
        var isClosed = await context.DailyEntries.AnyAsync(d =>
            d.GroupId == groupId && d.ActivatedAt != null && d.Date > date
        );
        if (isClosed && AuthorGuessRules.AuthorOf(entry.Question) is int authorId)
            result.Author = await context
                .Users.Where(u => u.Id == authorId)
                .Select(u => new VoterDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    FrameColor = u.FrameColor,
                })
                .FirstOrDefaultAsync();

        if (!entry.Question.Votes.Any(v => v.UserId == userId))
        {
            result.Locked = true;
            result.Results = [];
            result.FreeTextResponses = [];
            // The open question is voted from the group screen, not from here.
            if (isClosed)
                result.LateVoteQuestion = mapper.Map<QuestionToVoteDto>(entry.Question);
        }

        return result;
    }

    // ==========================================
    // GET HISTORY
    // Lista paginada de preguntas ya cerradas (cursor-based, más recientes primero).
    // Excluye la pregunta activa de hoy y las preseleccionadas futuras.
    // ==========================================
    public async Task<HistoryPageDto> GetHistoryAsync(int groupId, int userId, DateOnly? before, int pageSize = 20)
    {
        // The "open" entry is the most-recently-activated one. The lifecycle is driven by
        // ActivatedAt, not the calendar date, so the open entry's Date may be before today.
        // We must exclude it from history — only entries that have been superseded by a
        // newer activation are truly closed.
        var openDate = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .MaxAsync(d => (DateOnly?)d.Date);

        if (openDate == null)
            return new HistoryPageDto { Items = [], HasMore = false };

        var rows = await context
            .DailyEntries.Where(d =>
                d.GroupId == groupId && d.ActivatedAt != null && d.Date < openDate
            )
            .Where(d => before == null || d.Date < before)
            .OrderByDescending(d => d.Date)
            .Take(pageSize + 1)
            .Select(d => new
            {
                d.Date,
                d.Question.Text,
                d.Question.TextTranslations,
                d.Question.Type,
                TotalVotes = d.Question.Votes.Select(v => v.UserId).Distinct().Count(),
                AuthorUsername = d.Question.Source == QuestionSource.UserCreated && d.Question.Creator != null
                    ? d.Question.Creator.Username
                    : null,
                MyGuessCorrect = context
                    .AuthorGuesses.Where(g =>
                        g.DailyEntryId == d.Id && g.UserId == userId && g.GuessedCreatorId != null
                    )
                    .Select(g => (bool?)g.IsCorrect)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var items = rows.Select(d => new HistoryEntryDto
            {
                Date = d.Date,
                QuestionText = ContentTranslations.Pick(d.Text, d.TextTranslations),
                Type = d.Type,
                TotalVotes = d.TotalVotes,
                AuthorUsername = d.AuthorUsername,
                MyGuessCorrect = d.MyGuessCorrect,
            })
            .ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
            items.RemoveAt(items.Count - 1);

        return new HistoryPageDto { Items = items, HasMore = hasMore };
    }

    // ==========================================
    // CREATE
    // Añade una pregunta al pool sin activarla
    // ==========================================
    public async Task<(QuestionDto? question, string? error)> CreateAsync(
        int groupId,
        int creatorId,
        CreateQuestionDto dto
    )
    {
        // DB-dependent membership validation (§9), shared with the inline-create path.
        var memberIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();

        var membershipError = QuestionMembershipValidator.Validate(dto, memberIds, creatorId);
        if (membershipError != null)
            return (null, membershipError);

        var unusedByCreator = await context.Questions.CountAsync(q =>
            q.GroupId == groupId && !q.IsUsed && q.CreatorId == creatorId
        );
        if (unusedByCreator >= MaxUnusedQuestionsPerUserPerGroup)
            return (null, ServerText.UnusedQuestionsLimit(MaxUnusedQuestionsPerUserPerGroup));

        var unusedInGroup = await context.Questions.CountAsync(q =>
            q.GroupId == groupId && !q.IsUsed
        );
        if (unusedInGroup >= MaxUnusedQuestionsPerGroup)
            return (null, ServerText.GroupPoolFull);

        var question = new Question
        {
            Text = dto.Text,
            Type = dto.Type,
            Source = QuestionSource.UserCreated,
            IsUsed = false,
            CreatorId = creatorId,
            GroupId = groupId,
            DateCreated = DateTime.UtcNow,
            Metadata = QuestionMetadataBuilder.Build(dto),
        };

        if (dto.Type == QuestionType.CustomPoll && dto.Options?.Count > 0)
        {
            question.Options = dto.Options.Select(o => new Option { Text = o.Text }).ToList();
        }

        context.Questions.Add(question);
        await context.SaveChangesAsync();

        return (ToDto(question, creatorId), null);
    }

    private QuestionDto ToDto(Question question, int userId)
    {
        var dto = mapper.Map<QuestionDto>(question);
        dto.IsMine = question.CreatorId == userId;
        return dto;
    }

    // ==========================================
    // DELETE
    // Elimina una pregunta del pool (no usada) si el solicitante es el creador o el admin del grupo
    // ==========================================
    public async Task<string?> DeleteAsync(int groupId, int userId, int questionId)
    {
        var question = await context
            .Questions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.GroupId == groupId);

        if (question == null)
            return "not_found";

        if (question.IsUsed)
            return "in_use";

        var isGroupAdmin = await context.GroupMembers.AnyAsync(
            gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsAdmin);

        if (question.CreatorId != userId && !isGroupAdmin)
            return "forbidden";

        context.Questions.Remove(question);
        await context.SaveChangesAsync();

        return null;
    }
}
