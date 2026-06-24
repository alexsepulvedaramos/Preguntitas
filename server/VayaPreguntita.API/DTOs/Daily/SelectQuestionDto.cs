// DTOs/Daily/SelectQuestionDto.cs

using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

public class SelectQuestionDto
{
    // Si elige una del pool existente
    public int? ExistingQuestionId { get; set; }

    // Si elige una plantilla del pack base (se clona al grupo)
    public int? TemplateId { get; set; }

    // Si la crea en el momento
    public CreateQuestionDto? NewQuestion { get; set; }

    // Companion to ExistingQuestionId: reassign Deathmatch teams on a pool question
    // without creating a new entity. Also allows IsUsed=true (re-editing the pending DM).
    public List<List<int>>? TeamsOverride { get; set; }

    // Companion to NewQuestion: update an existing question's content in place instead of
    // creating a new entity. Used when the selector edits a pending Scale/CustomPoll.
    public int? OverwriteQuestionId { get; set; }
}
