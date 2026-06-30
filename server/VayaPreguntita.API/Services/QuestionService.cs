using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

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
        QuestionType? type = null,
        int? before = null,
        int pageSize = 12
    )
    {
        var query = context.Questions.Where(q => q.GroupId == groupId && !q.IsUsed);

        if (type != null)
            query = query.Where(q => q.Type == type);

        if (before != null)
            query = query.Where(q => q.Id < before);

        var items = await query
            .OrderByDescending(q => q.Id)
            .Take(pageSize + 1)
            .Select(q => mapper.Map<QuestionDto>(q))
            .ToListAsync();

        var hasMore = items.Count > pageSize;
        if (hasMore)
            items.RemoveAt(items.Count - 1);

        return new QuestionPageDto { Items = items, HasMore = hasMore };
    }

    // ==========================================
    // GET BY DATE
    // Devuelve la pregunta y resultados de una fecha concreta (historial)
    // ==========================================
    public async Task<QuestionResultDto?> GetByDateAsync(int groupId, DateOnly date)
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
            .FirstOrDefaultAsync(d => d.GroupId == groupId && d.Date == date);

        if (entry == null)
            return null;

        var usersById = await ResultsBuilder.BuildDeathmatchUsersById(context, entry.Question);

        return ResultsBuilder.Build(entry.Question, entry.Question.Votes, mapper, usersById);
    }

    // ==========================================
    // GET HISTORY
    // Lista paginada de preguntas ya cerradas (cursor-based, más recientes primero).
    // Excluye la pregunta activa de hoy y las preseleccionadas futuras.
    // ==========================================
    public async Task<HistoryPageDto> GetHistoryAsync(int groupId, DateOnly? before, int pageSize = 20)
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

        var items = await context
            .DailyEntries.Where(d =>
                d.GroupId == groupId && d.ActivatedAt != null && d.Date < openDate
            )
            .Where(d => before == null || d.Date < before)
            .OrderByDescending(d => d.Date)
            .Take(pageSize + 1)
            .Select(d => new HistoryEntryDto
            {
                Date = d.Date,
                QuestionText = d.Question.Text,
                Type = d.Question.Type,
                TotalVotes = d.Question.Votes.Select(v => v.UserId).Distinct().Count(),
            })
            .ToListAsync();

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
            return (
                null,
                $"You already have {MaxUnusedQuestionsPerUserPerGroup} unused questions in this group's pool."
            );

        var unusedInGroup = await context.Questions.CountAsync(q =>
            q.GroupId == groupId && !q.IsUsed
        );
        if (unusedInGroup >= MaxUnusedQuestionsPerGroup)
            return (null, "This group's question pool is full.");

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

        return (mapper.Map<QuestionDto>(question), null);
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
