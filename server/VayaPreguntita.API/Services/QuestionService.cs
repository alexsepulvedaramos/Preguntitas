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
    // ==========================================
    // GET POOL
    // Devuelve las preguntas disponibles del grupo (no usadas)
    // ==========================================
    public async Task<IEnumerable<QuestionDto>> GetPoolAsync(int groupId)
    {
        return await context
            .Questions.Where(q => q.GroupId == groupId && !q.IsUsed)
            .OrderByDescending(q => q.DateCreated)
            .Select(q => mapper.Map<QuestionDto>(q))
            .ToListAsync();
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

        return ResultsBuilder.Build(entry.Question, entry.Question.Votes, mapper);
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

}
