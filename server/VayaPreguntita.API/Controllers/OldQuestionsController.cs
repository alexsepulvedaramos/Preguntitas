namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OldQuestionsController(AppDbContext context, IMapper mapper) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    // Sentinel value for a "Nobody" selection in Superlative votes.
    private const int NobodyUserId = 0;

    // ==========================================
    // GET: api/questions?groupId=5
    // ==========================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionToVoteDto>>> GetDailyQuestion(
        [FromQuery] int groupId
    )
    {
        var questions = await _context
            .Questions.Where(q => q.GroupId == groupId)
            .Where(q => q.DateAsked != null)
            .ProjectTo<QuestionToVoteDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(questions);
    }

    // ==========================================
    // GET: api/questions/5
    // ==========================================
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetQuestion(int id)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        // 2. THE QUICK CHECK: Did this user already vote?
        bool hasVoted = await _context.Votes.AnyAsync(v =>
            v.QuestionId == id && v.UserId == currentUserId
        );

        // 3. FETCH BASE DATA: We always need the question and its options
        var question = await _context
            .Questions.Include(q => q.Options)
            .SingleOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        // 4. THE SMART ROUTING
        if (!hasVoted)
        {
            // The user hasn't voted yet. We just map to the voting DTO.
            var voteDto = _mapper.Map<QuestionToVoteDto>(question);
            return Ok(voteDto);
        }
        else
        {
            // The user has already voted. We need to calculate the results before returning.
            var resultDto = await CalculateResultsAsync(question);
            return Ok(resultDto);
        }
    }

    // ==========================================
    // POST: api/questions (Create a new question)
    // ==========================================
    [HttpPost]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(CreateQuestionDto createQuestionDto)
    {
        // 1. Validate Foreign Keys (Defensive Programming)
        var groupExists = await _context.Groups.AnyAsync(g => g.Id == createQuestionDto.GroupId);
        if (!groupExists)
            return BadRequest("Invalid Group ID.");
        var creatorExists = await _context.Users.AnyAsync(u => u.Id == createQuestionDto.CreatorId);
        if (!creatorExists)
            return BadRequest("Invalid Creator ID.");

        // 2. Validate Business Logic based on Question Type
        switch (createQuestionDto.Type)
        {
            case QuestionType.CustomPoll:
                if (createQuestionDto.Options == null || createQuestionDto.Options.Count < 2)
                {
                    return BadRequest("Custom Poll questions require options.");
                }
                break;

            case QuestionType.Superlative:
            case QuestionType.Scale:
            case QuestionType.SecretPairing:
            case QuestionType.Deathmatch:
                createQuestionDto.Options.Clear();
                break;

            default:
                return BadRequest("Unknown question type.");
        }

        // 3. Map CreateQuestionDto to a new Question entity using _mapper
        var question = _mapper.Map<Question>(createQuestionDto);

        // 4. Add the new entity to _context.Questions
        _context.Questions.Add(question);

        // 5. Save changes to the database asynchronously
        await _context.SaveChangesAsync();

        // 6. Map the saved entity back to a QuestionDto to return to the client
        var questionDto = _mapper.Map<QuestionDto>(question);

        // 7. Return a 201 Created response
        return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, questionDto);
    }

    // ==========================================
    // POST: api/questions/{id}/vote
    // ==========================================
    [HttpPost("{id}/vote")]
    public async Task<ActionResult> VoteQuestion(int id, CreateVoteDto createVoteDto)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        // 2. Fetch the question including its options (to validate the vote)
        var question = await _context
            .Questions.Include(q => q.Options)
            .SingleOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound("Question not found.");

        // Validate that the current user is a member of the question's group
        var isVoterInGroup = await _context.Groups.AnyAsync(g =>
            g.Id == question.GroupId && g.Users.Any(u => u.Id == currentUserId)
        );

        if (!isVoterInGroup)
            return Forbid();

        // 3. Check if user already voted
        var alreadyVoted = await _context.Votes.AnyAsync(v =>
            v.QuestionId == id && v.UserId == currentUserId
        );

        if (alreadyVoted)
            return BadRequest("User has already voted on this question.");

        int? selectedTeamLeaderId = null;

        // 4. Validate the Vote payload against the Question Type
        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                if (
                    createVoteDto.SelectedOptionIds == null
                    || createVoteDto.SelectedOptionIds.Count == 0
                )
                    return BadRequest("You must select at least one option.");

                var pollMinSelections =
                    question.Metadata.MinSelections > 0 ? question.Metadata.MinSelections : 1;
                var pollMaxSelections =
                    question.Metadata.MaxSelections > 0
                        ? question.Metadata.MaxSelections
                        : pollMinSelections;

                if (
                    createVoteDto.SelectedOptionIds.Count < pollMinSelections
                    || createVoteDto.SelectedOptionIds.Count > pollMaxSelections
                )
                {
                    return BadRequest("Selection count is outside the allowed range.");
                }

                var allOptionsValid = createVoteDto.SelectedOptionIds.All(selectedId =>
                    question.Options.Any(o => o.Id == selectedId)
                );

                if (!allOptionsValid)
                    return BadRequest("One or more selected options are invalid.");
                break;

            case QuestionType.Deathmatch:
                if (
                    createVoteDto.SelectedTargetUserIds == null
                    || createVoteDto.SelectedTargetUserIds.Count == 0
                )
                    return BadRequest("You must select a team for this question type.");

                if (question.Metadata.Teams.Count != 2)
                    return BadRequest("Deathmatch teams are not configured correctly.");

                var normalizedSelection = createVoteDto
                    .SelectedTargetUserIds.OrderBy(id => id)
                    .ToList();

                var matchedTeam = question.Metadata.Teams.FirstOrDefault(team =>
                    team.MemberIds.OrderBy(id => id).SequenceEqual(normalizedSelection)
                );

                if (matchedTeam == null)
                    return BadRequest("Selected team does not match the configured teams.");

                selectedTeamLeaderId = matchedTeam.MemberIds.First();
                break;

            case QuestionType.Superlative:
                if (createVoteDto.SelectedTargetUserId == null)
                    return BadRequest("You must select a target user for this question type.");

                if (createVoteDto.SelectedTargetUserId == NobodyUserId)
                {
                    if (!question.Metadata.AllowNobody)
                    {
                        return BadRequest("Selecting nobody is not allowed for this question.");
                    }

                    break;
                }

                if (
                    question.Metadata.BlacklistedUserIds.Contains(
                        createVoteDto.SelectedTargetUserId.Value
                    )
                )
                    return BadRequest("Selected user is blacklisted for this question.");

                // Check group membership (implies existence)
                var isTargetUserInGroup = await _context.Groups.AnyAsync(g =>
                    g.Id == question.GroupId
                    && g.Users.Any(u => u.Id == createVoteDto.SelectedTargetUserId.Value)
                );

                if (!isTargetUserInGroup)
                    return BadRequest("Selected target user does not belong to this group.");
                break;

            case QuestionType.Scale:
                if (createVoteDto.NumericValue == null)
                    return BadRequest("You must provide a numeric value for this question type.");

                var rangeMin = question.Metadata.RangeMin ?? 1;
                var rangeMax = question.Metadata.RangeMax ?? 10;

                if (
                    createVoteDto.NumericValue.Value < rangeMin
                    || createVoteDto.NumericValue.Value > rangeMax
                )
                    return BadRequest("Numeric value is outside the allowed range.");
                break;

            case QuestionType.SecretPairing:
                if (
                    createVoteDto.SelectedTargetUserIds == null
                    || createVoteDto.SelectedTargetUserIds.Count == 0
                )
                    return BadRequest("You must select target users for this question type.");

                var pairingMinSelections =
                    question.Metadata.MinSelections > 0 ? question.Metadata.MinSelections : 2;
                var pairingMaxSelections =
                    question.Metadata.MaxSelections > 0 ? question.Metadata.MaxSelections : 2;

                if (
                    createVoteDto.SelectedTargetUserIds.Count < pairingMinSelections
                    || createVoteDto.SelectedTargetUserIds.Count > pairingMaxSelections
                )
                    return BadRequest("Selection count is outside the allowed range.");

                var hasDuplicateSelections =
                    createVoteDto.SelectedTargetUserIds.Distinct().Count()
                    != createVoteDto.SelectedTargetUserIds.Count;

                if (hasDuplicateSelections)
                    return BadRequest("Selected target users must be unique.");

                var allTargetsInGroup = await _context.Groups.AnyAsync(g =>
                    g.Id == question.GroupId
                    && createVoteDto.SelectedTargetUserIds.All(id => g.Users.Any(u => u.Id == id))
                );

                if (!allTargetsInGroup)
                    return BadRequest("One or more selected users do not belong to this group.");
                break;

            default:
                return BadRequest("Unknown question type.");
        }

        // 5. Map DTO to Vote Entities (Handling multiple inserts)
        var votesToInsert = new List<Vote>();

        if (question.Type == QuestionType.CustomPoll)
        {
            foreach (var optionId in createVoteDto.SelectedOptionIds!)
            {
                var vote = _mapper.Map<Vote>(createVoteDto);
                vote.UserId = currentUserId;
                vote.QuestionId = id;
                vote.SelectedOptionId = optionId;

                votesToInsert.Add(vote);
            }
        }
        else if (question.Type == QuestionType.Deathmatch)
        {
            var vote = _mapper.Map<Vote>(createVoteDto);
            vote.UserId = currentUserId;
            vote.QuestionId = id;
            vote.SelectedTargetUserId = selectedTeamLeaderId;

            votesToInsert.Add(vote);
        }
        else if (question.Type == QuestionType.SecretPairing)
        {
            foreach (var targetUserId in createVoteDto.SelectedTargetUserIds!)
            {
                var vote = _mapper.Map<Vote>(createVoteDto);
                vote.UserId = currentUserId;
                vote.QuestionId = id;
                vote.SelectedTargetUserId = targetUserId;

                votesToInsert.Add(vote);
            }
        }
        else
        {
            var vote = _mapper.Map<Vote>(createVoteDto);
            vote.UserId = currentUserId;
            vote.QuestionId = id;

            if (
                question.Type == QuestionType.Superlative
                && createVoteDto.SelectedTargetUserId == NobodyUserId
            )
            {
                vote.SelectedTargetUserId = null;
            }

            votesToInsert.Add(vote);
        }

        // 6. Save to database
        _context.Votes.AddRange(votesToInsert);
        await _context.SaveChangesAsync();

        return Ok("Vote registered successfully.");
    }

    private bool TryGetCurrentUserId(out int currentUserId)
    {
        currentUserId = 0;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out currentUserId);
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================

    // Calculates the voting percentages based on the specific Question Type
    private async Task<QuestionResultDto> CalculateResultsAsync(Question question)
    {
        var resultDto = _mapper.Map<QuestionResultDto>(question);

        // 1. Fetch all votes for this specific question
        // Hint: You might need to Include(v => v.SelectedTargetUser) for the Superlative type later!
        var allVotes = await _context
            .Votes.Include(v => v.User)
            .Include(v => v.SelectedTargetUser)
            .Where(v => v.QuestionId == question.Id)
            .ToListAsync();

        // 2. Set the total amount of votes globally
        resultDto.TotalVotes = allVotes.Count;

        if (resultDto.TotalVotes == 0)
            return resultDto;

        // 3. Route the calculation logic based on the Question Type
        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var option in question.Options)
                {
                    // 1. Logic and calculations
                    var optionVotes = allVotes.Where(v => v.SelectedOptionId == option.Id).ToList();
                    var voteCount = optionVotes.Count;
                    var percentage =
                        resultDto.TotalVotes > 0
                            ? (double)voteCount / resultDto.TotalVotes * 100
                            : 0;

                    // 2. Mapping and assignment
                    var optionResult = _mapper.Map<OptionResultDto>(option);
                    optionResult.VoteCount = voteCount;
                    optionResult.Percentage = percentage;
                    optionResult.Voters = _mapper.Map<List<VoterDto>>(optionVotes);

                    resultDto.Results.Add(optionResult);
                }
                break;

            case QuestionType.Deathmatch:
                for (var teamIndex = 0; teamIndex < question.Metadata.Teams.Count; teamIndex++)
                {
                    var team = question.Metadata.Teams[teamIndex];
                    var teamVotes = allVotes
                        .Where(v =>
                            v.SelectedTargetUserId.HasValue
                            && team.MemberIds.Contains(v.SelectedTargetUserId.Value)
                        )
                        .ToList();
                    var voteCount = teamVotes.Count;
                    var percentage =
                        resultDto.TotalVotes > 0
                            ? (double)voteCount / resultDto.TotalVotes * 100
                            : 0;

                    var optionResult = new OptionResultDto
                    {
                        Id = teamIndex + 1,
                        DisplayText = $"Team {teamIndex + 1}",
                        VoteCount = voteCount,
                        Percentage = percentage,
                        Voters = _mapper.Map<List<VoterDto>>(teamVotes),
                    };

                    resultDto.Results.Add(optionResult);
                }
                break;

            case QuestionType.Superlative:
            case QuestionType.SecretPairing:
                var targetUserGroups = allVotes.GroupBy(v => v.SelectedTargetUserId);

                foreach (var group in targetUserGroups)
                {
                    // 1. Logic and calculations
                    var groupVotes = group.ToList();
                    var voteCount = groupVotes.Count;
                    var percentage =
                        resultDto.TotalVotes > 0
                            ? (double)voteCount / resultDto.TotalVotes * 100
                            : 0;
                    var targetUser = group.First().SelectedTargetUser;
                    var displayName =
                        group.Key == null ? "Nobody" : targetUser?.Username ?? "Unknown User";

                    // 2. Mapping and assignment (Manually created since there's no Option entity)
                    var optionResult = new OptionResultDto
                    {
                        Id = group.Key ?? 0,
                        DisplayText = displayName,
                        TargetUser = _mapper.Map<UserDto>(targetUser),
                        VoteCount = voteCount,
                        Percentage = percentage,
                        Voters = _mapper.Map<List<VoterDto>>(groupVotes),
                    };

                    resultDto.Results.Add(optionResult);
                }
                break;

            case QuestionType.Scale:
                // Group votes by their numeric value (e.g., all "8s", all "10s")
                var scaleGroups = allVotes
                    .Where(v => v.NumericValue.HasValue)
                    .GroupBy(v => v.NumericValue!.Value)
                    .OrderBy(g => g.Key); // Sort by number (1, 2, 3...) for the chart

                foreach (var group in scaleGroups)
                {
                    // 1. Logic and calculations
                    var groupVotes = group.ToList();
                    var voteCount = groupVotes.Count;
                    var percentage =
                        resultDto.TotalVotes > 0
                            ? (double)voteCount / resultDto.TotalVotes * 100
                            : 0;

                    // 2. Mapping and assignment
                    var optionResult = new OptionResultDto
                    {
                        Id = group.Key, // The number itself serves as the ID
                        DisplayText = group.Key.ToString(),
                        VoteCount = voteCount,
                        Percentage = percentage,
                        Voters = _mapper.Map<List<VoterDto>>(groupVotes),
                    };

                    resultDto.Results.Add(optionResult);
                }
                break;

            default:
                // Always good practice to handle unknown types gracefully
                break;
        }

        return resultDto;
    }
}
