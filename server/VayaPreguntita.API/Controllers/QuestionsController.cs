namespace VayaPreguntita.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuestionsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/questions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions()
    {
        // Retrieve questions from the database including their related answers
        var questions = await _context.Questions.Include(q => q.Answers).ToListAsync();

        // Map the list of entities to a list of DTOs
        return Ok(
            questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Creator = q.Creator,
                // Map the answers for each question
                Answers =
                [
                    .. q.Answers.Select(a => new AnswerDto
                    {
                        Id = a.Id,
                        Text = a.Text,
                        Creator = a.Creator,
                        DateCreated = a.DateCreated,
                    }),
                ],
            })
        );
    }

    // GET: api/questions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionDto>> GetQuestion(int id)
    {
        var question = await _context
            .Questions.Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        // Manually map the Entity to our Output DTO
        var questionDto = new QuestionDto
        {
            Id = question.Id,
            Text = question.Text,
            Creator = question.Creator,
            DateAsked = question.DateAsked,
            Type = question.Type,
            // Map the answers list to AnswerDto
            Answers =
            [
                .. question.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Text = a.Text,
                    Creator = a.Creator,
                    DateCreated = a.DateCreated,
                }),
            ],
        };

        return Ok(questionDto);
    }

    // POST: api/questions
    [HttpPost]
    public async Task<ActionResult<Question>> PostQuestion(CreateQuestionDto questionDto)
    {
        var question = new Question
        {
            Text = questionDto.Text,
            Creator = questionDto.Creator,
            Type = questionDto.Type,
            DateCreated = DateTime.UtcNow,

            Answers =
            [
                .. questionDto.Answers.Select(a => new Answer
                {
                    Text = a.Text,
                    Creator = a.Creator,
                    DateCreated = DateTime.UtcNow,
                }),
            ],
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        var responseDto = new QuestionDto
        {
            Id = question.Id,
            Text = question.Text,
            Creator = question.Creator,
            DateAsked = question.DateCreated,
            Type = question.Type,
            Answers =
            [
                .. question.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Text = a.Text,
                    Creator = a.Creator,
                    DateCreated = a.DateCreated,
                }),
            ],
        };

        return CreatedAtAction(nameof(GetQuestion), new { id = responseDto.Id }, responseDto);
    }
}
