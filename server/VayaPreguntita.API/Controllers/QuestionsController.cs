namespace VayaPreguntita.API.Controllers;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController(AppDbContext context, IMapper mapper) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    // ==========================================
    // GET: api/questions?groupId=5
    // ==========================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionToVoteDto>>> GetQuestions(
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
        // 1. HARDCODED USER (Until we implement JWT tokens)
        int currentUserId = 1;

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
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
                if (createQuestionDto.Options == null || createQuestionDto.Options.Count < 2)
                {
                    return BadRequest(
                        "Single Choice and Multiple Choice questions require at least 2 options."
                    );
                }
                break;

            case QuestionType.TargetUser:
            case QuestionType.Scale:
            case QuestionType.FreeText:
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
        // 1. HARDCODED USER (Until JWT)
        int currentUserId = 1;

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

        // 4. Validate the Vote payload against the Question Type
        // 4. Validate the Vote payload against the Question Type
        switch (question.Type)
        {
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
                if (
                    createVoteDto.SelectedOptionIds == null
                    || createVoteDto.SelectedOptionIds.Count == 0
                )
                    return BadRequest("You must select at least one option.");

                if (
                    question.Type == QuestionType.SingleChoice
                    && createVoteDto.SelectedOptionIds.Count > 1
                )
                    return BadRequest("You can only select one option for this question.");

                var allOptionsValid = createVoteDto.SelectedOptionIds.All(selectedId =>
                    question.Options.Any(o => o.Id == selectedId)
                );

                if (!allOptionsValid)
                    return BadRequest("One or more selected options are invalid.");
                break;

            case QuestionType.TargetUser:
                if (createVoteDto.SelectedTargetUserId == null)
                    return BadRequest("You must select a target user for this question type.");

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

                // Optional: Check min/max values here if defined in your Question entity
                break;

            case QuestionType.FreeText:
                if (string.IsNullOrWhiteSpace(createVoteDto.FreeText))
                    return BadRequest("Free text response cannot be empty.");
                break;

            default:
                return BadRequest("Unknown question type.");
        }

        // 5. Map DTO to Vote Entities (Handling multiple inserts)
        var votesToInsert = new List<Vote>();

        if (
            question.Type == QuestionType.SingleChoice
            || question.Type == QuestionType.MultipleChoice
        )
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
        else
        {
            var vote = _mapper.Map<Vote>(createVoteDto);
            vote.UserId = currentUserId;
            vote.QuestionId = id;

            votesToInsert.Add(vote);
        }

        // 6. Save to database
        _context.Votes.AddRange(votesToInsert);
        await _context.SaveChangesAsync();

        return Ok("Vote registered successfully.");
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
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
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

            case QuestionType.TargetUser:
                var targetUserGroups = allVotes
                    .Where(v => v.SelectedTargetUserId != null)
                    .GroupBy(v => v.SelectedTargetUserId);

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

                    // 2. Mapping and assignment (Manually created since there's no Option entity)
                    var optionResult = new OptionResultDto
                    {
                        Id = group.Key ?? 0,
                        DisplayText = targetUser?.Username ?? "Unknown User",
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

            case QuestionType.FreeText:
                // TODO (Later): Just grab all the FreeText strings and list them.
                // No percentages needed here!
                foreach (var vote in allVotes.Where(v => !string.IsNullOrEmpty(v.FreeText)))
                {
                    var optionResult = new OptionResultDto
                    {
                        Id = vote.Id,
                        DisplayText = vote.FreeText!,
                        Voters = [_mapper.Map<VoterDto>(vote)],
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
