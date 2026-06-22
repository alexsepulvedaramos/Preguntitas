using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class DailyService(AppDbContext context, IMapper mapper) : IDailyService
{
    // ==========================================
    // GET CURRENT STATUS
    // ==========================================
    public async Task<DailyStatusDto> GetCurrentStatusAsync(int groupId, int userId)
    {
        var today = DailyClock.Today();

        var group =
            await context
                .Groups.Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        var selector = CalculateSelector(group.Members, group.DateCreated, today);

        var dailyEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(d => d.GroupId == groupId && d.Date == today);

        // No hay entrada para hoy todavía
        if (dailyEntry == null)
        {
            return new DailyStatusDto
            {
                Status = "no_question",
                SelectorUserId = selector.UserId,
                SelectorUsername = selector.User.Username,
                IsCurrentUserSelector = selector.UserId == userId,
            };
        }

        // Hay entrada pero aún no está activada (el selector puede cambiarla)
        if (dailyEntry.ActivatedAt == null)
        {
            return new DailyStatusDto
            {
                Status = "pending_selection",
                SelectorUserId = selector.UserId,
                SelectorUsername = selector.User.Username,
                IsCurrentUserSelector = selector.UserId == userId,
                Question = mapper.Map<QuestionToVoteDto>(dailyEntry.Question),
            };
        }

        // Hay pregunta activa — ¿ha votado el usuario?
        var hasVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == dailyEntry.QuestionId && v.UserId == userId
        );

        if (!hasVoted)
        {
            return new DailyStatusDto
            {
                Status = "voting",
                SelectorUserId = selector.UserId,
                SelectorUsername = selector.User.Username,
                IsCurrentUserSelector = selector.UserId == userId,
                Question = mapper.Map<QuestionToVoteDto>(dailyEntry.Question),
            };
        }

        // Ya votó — calculamos resultados
        var results = await CalculateResultsAsync(dailyEntry.Question);
        return new DailyStatusDto
        {
            Status = "results",
            SelectorUserId = selector.UserId,
            SelectorUsername = selector.User.Username,
            IsCurrentUserSelector = selector.UserId == userId,
            Question = mapper.Map<QuestionToVoteDto>(dailyEntry.Question),
            Results = results,
        };
    }

    // ==========================================
    // GET SELECTION SOURCES (pool del grupo + pack base)
    // ==========================================
    public async Task<SelectionSourcesDto> GetSelectionSourcesAsync(int groupId)
    {
        var pool = await context
            .Questions.Where(q => q.GroupId == groupId && !q.IsUsed)
            .OrderByDescending(q => q.DateCreated)
            .Select(q => new SelectionSourceItemDto
            {
                SourceType = "pool",
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
            })
            .ToListAsync();

        var pack = await context
            .QuestionTemplates.Where(t => t.Pack.IsActiveByDefault)
            .OrderBy(t => t.Type)
            .Select(t => new SelectionSourceItemDto
            {
                SourceType = "pack",
                Id = t.Id,
                Text = t.Text,
                Type = t.Type,
            })
            .ToListAsync();

        return new SelectionSourcesDto { Pool = pool, Pack = pack };
    }

    // ==========================================
    // SELECT QUESTION (el selector elige o crea)
    // ==========================================
    public async Task<SelectResult> SelectQuestionAsync(
        int groupId,
        int userId,
        SelectQuestionDto dto
    )
    {
        var today = DailyClock.Today();

        var group =
            await context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        // Verificar que es el turno de este usuario
        var selector = CalculateSelector(group.Members, group.DateCreated, today);
        if (selector.UserId != userId)
            return SelectResult.NotYourTurn;

        var dailyEntry = await context.DailyEntries.FirstOrDefaultAsync(d =>
            d.GroupId == groupId && d.Date == today
        );

        // Una vez activada no se puede cambiar
        if (dailyEntry?.ActivatedAt != null)
            return SelectResult.AlreadyActivated;

        Question question;

        if (dto.NewQuestion != null)
        {
            // Crea la pregunta en el momento y la activa directamente
            question = new Question
            {
                Text = dto.NewQuestion.Text,
                Type = dto.NewQuestion.Type,
                CreatorId = userId,
                GroupId = groupId,
                IsUsed = true,
                DateActivated = DateTime.UtcNow,
                Source = QuestionSource.UserCreated,
                Metadata = QuestionMetadataBuilder.Build(dto.NewQuestion),
            };

            if (dto.NewQuestion.Options?.Count > 0)
            {
                question.Options = dto
                    .NewQuestion.Options.Select(o => new Option { Text = o.Text })
                    .ToList();
            }

            context.Questions.Add(question);
            await context.SaveChangesAsync();
        }
        else if (dto.ExistingQuestionId.HasValue)
        {
            // Elige una del pool
            question =
                await context.Questions.FirstOrDefaultAsync(q =>
                    q.Id == dto.ExistingQuestionId.Value && q.GroupId == groupId && !q.IsUsed
                ) ?? null!;

            if (question == null)
                return SelectResult.QuestionNotFound;

            question.IsUsed = true;
            question.DateActivated = DateTime.UtcNow;
        }
        else if (dto.TemplateId.HasValue)
        {
            // Clona una plantilla del pack base al grupo y la activa
            var template = await context
                .QuestionTemplates.Include(t => t.Options)
                .FirstOrDefaultAsync(t => t.Id == dto.TemplateId.Value);

            if (template == null)
                return SelectResult.QuestionNotFound;

            var memberIds = group.Members.Select(m => m.UserId).ToList();
            question = TemplateCloner.CloneToGroup(template, groupId, memberIds);
            question.IsUsed = true;
            question.DateActivated = DateTime.UtcNow;
            context.Questions.Add(question);
            await context.SaveChangesAsync();
        }
        else
        {
            return SelectResult.QuestionNotFound;
        }

        // Crear o actualizar el DailyEntry
        if (dailyEntry == null)
        {
            dailyEntry = new DailyEntry
            {
                GroupId = groupId,
                Date = today,
                SelectorUserId = userId,
                QuestionId = question.Id,
                ActivatedAt = DateTime.UtcNow,
                IsAutoSelected = false,
            };
            context.DailyEntries.Add(dailyEntry);
        }
        else
        {
            // Cambia la preselección automática por la elección manual
            var oldQuestionId = dailyEntry.QuestionId;
            var wasAutoSelected = dailyEntry.IsAutoSelected;
            dailyEntry.QuestionId = question.Id;
            dailyEntry.ActivatedAt = DateTime.UtcNow;
            dailyEntry.IsAutoSelected = false;

            // Soltar la pregunta preseleccionada automáticamente si era diferente
            if (oldQuestionId != question.Id && wasAutoSelected)
            {
                var oldQuestion = await context.Questions.FindAsync(oldQuestionId);
                if (oldQuestion != null)
                {
                    if (oldQuestion.Source == QuestionSource.Pack)
                    {
                        // Un clon de pack no tiene valor en el pool: se elimina
                        context.Questions.Remove(oldQuestion);
                    }
                    else
                    {
                        oldQuestion.IsUsed = false;
                        oldQuestion.DateActivated = null;
                    }
                }
            }
        }

        await context.SaveChangesAsync();
        return SelectResult.Success;
    }

    // ==========================================
    // VOTE
    // ==========================================
    public async Task<VoteResult> VoteAsync(int groupId, int userId, CreateVoteDto dto)
    {
        var today = DailyClock.Today();

        var dailyEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(d =>
                d.GroupId == groupId && d.Date == today && d.ActivatedAt != null
            );

        if (dailyEntry == null)
            return VoteResult.NoActiveQuestion;

        var alreadyVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == dailyEntry.QuestionId && v.UserId == userId
        );

        if (alreadyVoted)
            return VoteResult.AlreadyVoted;

        var question = dailyEntry.Question;
        var validationResult = ValidateVotePayload(dto, question);
        if (validationResult != null)
            return VoteResult.InvalidPayload;

        var votes = BuildVotes(dto, question, userId);
        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();

        return VoteResult.Success;
    }

    // ==========================================
    // PRESELECT FOR TOMORROW (llamado por el BackgroundService)
    // ==========================================
    public async Task PreselectForGroupAsync(int groupId, DateOnly date)
    {
        // Idempotente: si ya existe entrada para esa fecha, no hacer nada
        var exists = await context.DailyEntries.AnyAsync(d =>
            d.GroupId == groupId && d.Date == date
        );

        if (exists)
            return;

        var group = await context
            .Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return;

        var selector = CalculateSelector(group.Members, group.DateCreated, date);

        // Elegir pregunta aleatoria del pool del grupo
        var question = await context
            .Questions.Where(q => q.GroupId == groupId && !q.IsUsed)
            .OrderBy(_ => Guid.NewGuid()) // aleatorio
            .FirstOrDefaultAsync();

        if (question == null)
        {
            // Fallback: clonar una plantilla del pack base (§6 — siempre hay una pregunta)
            var template = await context
                .QuestionTemplates.Include(t => t.Options)
                .Where(t => t.Pack.IsActiveByDefault)
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (template == null)
                return; // No hay plantillas activas (no debería ocurrir con el pack base)

            var memberIds = group.Members.Select(m => m.UserId).ToList();
            question = TemplateCloner.CloneToGroup(template, groupId, memberIds);
            question.IsUsed = true;
            context.Questions.Add(question);
            await context.SaveChangesAsync(); // persistir para obtener el Id de la pregunta clonada
        }
        else
        {
            question.IsUsed = true;
        }
        // No ponemos DateActivated aquí — se pone cuando se activa de verdad

        context.DailyEntries.Add(
            new DailyEntry
            {
                GroupId = groupId,
                Date = date,
                QuestionId = question.Id,
                SelectorUserId = selector.UserId,
                IsAutoSelected = true,
                PreselectedAt = DateTime.UtcNow,
                ActivatedAt = null, // Se activará a la hora configurada del grupo
            }
        );

        await context.SaveChangesAsync();
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    private static GroupMember CalculateSelector(
        List<GroupMember> members,
        DateTime groupCreatedAt,
        DateOnly date
    )
    {
        var daysSinceCreation = date.DayNumber - DateOnly.FromDateTime(groupCreatedAt).DayNumber;
        var ordered = members.OrderBy(m => m.JoinedAt).ToList();
        var index = daysSinceCreation % ordered.Count;
        return ordered[index];
    }

    private static string? ValidateVotePayload(CreateVoteDto dto, Question question)
    {
        return question.Type switch
        {
            QuestionType.CustomPoll
                when dto.SelectedOptionIds == null || dto.SelectedOptionIds.Count == 0 =>
                "Must select at least one option.",

            QuestionType.CustomPoll
                when !dto.SelectedOptionIds!.All(id => question.Options.Any(o => o.Id == id)) =>
                "One or more selected options are invalid.",

            QuestionType.Superlative when dto.SelectedTargetUserId == null =>
                "Must select a target user.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId != 0
                    && question.Metadata.BlacklistedUserIds.Contains(
                        dto.SelectedTargetUserId!.Value
                    ) => "Selected user is blacklisted.",

            QuestionType.Scale when dto.NumericValue == null => "Must provide a numeric value.",

            QuestionType.Scale
                when dto.NumericValue < (question.Metadata.RangeMin ?? 1)
                    || dto.NumericValue > (question.Metadata.RangeMax ?? 10) =>
                "Numeric value out of range.",

            QuestionType.SecretPairing
                when dto.SelectedTargetUserIds == null || dto.SelectedTargetUserIds.Count != 2 =>
                "Must select exactly 2 users.",

            QuestionType.Deathmatch
                when dto.SelectedTargetUserIds == null
                    || !question.Metadata.Teams.Any(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual(dto.SelectedTargetUserIds.OrderBy(id => id))
                    ) => "Must select a valid team.",

            _ => null, // válido
        };
    }

    private static List<Vote> BuildVotes(CreateVoteDto dto, Question question, int userId)
    {
        var votes = new List<Vote>();

        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var optionId in dto.SelectedOptionIds!)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedOptionId = optionId,
                        }
                    );
                break;

            case QuestionType.Superlative:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        SelectedTargetUserId =
                            dto.SelectedTargetUserId == 0
                                ? null // Nobody
                                : dto.SelectedTargetUserId,
                    }
                );
                break;

            case QuestionType.Scale:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        NumericValue = dto.NumericValue,
                    }
                );
                break;

            case QuestionType.SecretPairing:
                foreach (var targetId in dto.SelectedTargetUserIds!)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = targetId,
                        }
                    );
                break;

            case QuestionType.Deathmatch:
                var matchedTeamLeader = question
                    .Metadata.Teams.FirstOrDefault(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual((dto.SelectedTargetUserIds ?? []).OrderBy(id => id))
                    )
                    ?.First();

                if (matchedTeamLeader.HasValue)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = matchedTeamLeader,
                        }
                    );
                break;
        }

        return votes;
    }

    // ==========================================
    // CALCULATE RESULTS
    // ==========================================
    private async Task<QuestionResultDto> CalculateResultsAsync(Question question)
    {
        var allVotes = await context
            .Votes.Include(v => v.User)
            .Include(v => v.SelectedTargetUser)
            .Where(v => v.QuestionId == question.Id)
            .ToListAsync();

        return ResultsBuilder.Build(question, allVotes, mapper);
    }
}
