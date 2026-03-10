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
    public async Task<ActionResult<QuestionToVoteDto>> PostQuestion(CreateQuestionDto questionDto)
    {
        // TODO 3: Map the incoming CreateQuestionDto to the Question entity.
        // var question = _mapper.Map<Question>(questionDto);

        // TODO 4: Hardcode the missing required database fields for now (Authentication comes later!).
        // question.CreatorId = 1;
        // question.GroupId = 1;

        // TODO 5: Add the question to the _context and SaveChangesAsync().

        // TODO 6: Map the newly saved question back to a QuestionToVoteDto.
        // var responseDto = ...

        // TODO 7: Return a CreatedAtAction pointing to GetQuestion.
        // return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, responseDto);

        return Ok(); // Placeholder so it compiles
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================

    // Calculates the voting percentages based on the specific Question Type
    private async Task<QuestionResultDto> CalculateResultsAsync(Question question)
    {
        var resultDto = _mapper.Map<QuestionResultDto>(question);

        // 1. Fetch all votes for this specific question
        // TODO: Write the EF Core query here.
        // Hint: You might need to Include(v => v.SelectedTargetUser) for the Superlative type later!
        // var allVotes = ...

        // 2. Set the total amount of votes globally
        // resultDto.TotalVotes = ...

        if (resultDto.TotalVotes == 0)
            return resultDto;

        // 3. Route the calculation logic based on the Question Type
        switch (question.Type)
        {
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
                // TODO: Here goes the logic we discussed!
                // Loop through question.Options, count votes where v.SelectedOptionId == option.Id,
                // calculate percentage, and add to resultDto.Results
                break;

            case QuestionType.TargetUser:
                // TODO (Later): Group allVotes by SelectedTargetUserId.
                // The "OptionResultDto.DisplayText" will be the Target User's name.
                break;

            case QuestionType.Scale:
                // TODO (Later): Calculate the mathematical average of all NumericValue fields.
                // Maybe return a single result with the average?
                break;

            case QuestionType.FreeText:
                // TODO (Later): Just grab all the FreeText strings and list them.
                // No percentages needed here!
                break;

            default:
                // Always good practice to handle unknown types gracefully
                break;
        }

        return resultDto;
    }
}
