namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionPageDto
{
    public List<QuestionDto> Items { get; set; } = [];
    public bool HasMore { get; set; }
}
