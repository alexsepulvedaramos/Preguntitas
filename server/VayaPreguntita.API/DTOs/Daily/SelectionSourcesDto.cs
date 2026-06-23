// DTOs/Daily/SelectionSourcesDto.cs

using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Daily;

public class SelectionSourceItemDto
{
    public string SourceType { get; set; } = string.Empty; // "pool" | "pack"
    public int Id { get; set; } // QuestionId (pool) | TemplateId (pack)
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }

    // Only populated for CustomPoll items, so the picker can preview the options.
    public List<string> Options { get; set; } = [];
}

public class SelectionSourcesDto
{
    public List<SelectionSourceItemDto> Pool { get; set; } = [];
    public List<SelectionSourceItemDto> Pack { get; set; } = [];
}
