using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Packs;

public class PackTemplateDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int PackId { get; set; }

    // Only populated for CustomPoll templates.
    public List<string> Options { get; set; } = [];
}

public class PackTemplatePageDto
{
    public List<PackTemplateDto> Items { get; set; } = [];
    public bool HasMore { get; set; }
}
