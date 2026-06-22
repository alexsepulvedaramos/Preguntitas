namespace VayaPreguntita.API.Entities;

public class Pack
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Whether this pack is active for every group by default (MVP: always true for "Base").
    public bool IsActiveByDefault { get; set; } = true;

    public List<QuestionTemplate> Templates { get; set; } = [];
}
