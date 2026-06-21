// DTOs/Daily/SelectQuestionDto.cs

using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

public class SelectQuestionDto
{
    // Si elige una del pool existente
    public int? ExistingQuestionId { get; set; }

    // Si la crea en el momento
    public CreateQuestionDto? NewQuestion { get; set; }
}
