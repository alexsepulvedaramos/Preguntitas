// Data/BasePackSeeder.cs
namespace VayaPreguntita.API.Data;

using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

// Idempotent runtime seeder for the always-on "Base" pack (spec §6).
// Runs at startup; if the Base pack already exists it does nothing.
public static class BasePackSeeder
{
    private const string BasePackName = "Base";

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Packs.AnyAsync(p => p.Name == BasePackName))
            return;

        var pack = new Pack
        {
            Name = BasePackName,
            Description = "Preguntas base disponibles para todos los grupos.",
            IsActiveByDefault = true,
            Templates = BuildTemplates(),
        };

        context.Packs.Add(pack);
        await context.SaveChangesAsync();
    }

    private static List<QuestionTemplate> BuildTemplates() =>
        [
            // ---------- Superlative (self-contained) ----------
            Superlative("¿Quién es más probable que acabe durmiendo en el sofá?"),
            Superlative("¿Quién llegaría tarde a su propio funeral?", allowNobody: true),
            Superlative("¿Quién se comería lo último de la nevera sin preguntar?"),
            // ---------- Deathmatch (teams auto-resolved at clone) ----------
            Deathmatch("Pelea a muerte entre dos equipos: ¿quién gana?"),
            Deathmatch("Apocalipsis zombi: ¿qué bando sobrevive?"),
            Deathmatch("Concurso de talentos: ¿qué equipo se lleva el premio?"),
            // ---------- Scale (target auto-resolved at clone) ----------
            Scale("Del 1 al 10, ¿cómo de 'rayada' está hoy esta persona?"),
            Scale("Del 1 al 10, ¿qué nota le pones a su último plan?"),
            Scale("Del 1 al 10, ¿cómo de probable es que llegue tarde hoy?"),
            // ---------- Secret Pairing ----------
            SecretPairing("¿Qué dos personas del grupo harían mejor pareja?"),
            SecretPairing("¿Qué dos personas montarían el mejor negocio juntas?"),
            SecretPairing("¿Qué dos personas se irían de viaje sin avisar a nadie?"),
            // ---------- Custom Poll ----------
            CustomPoll(
                "¿A qué hora quedamos?",
                ["A las 20:00", "A las 21:00", "A las 22:00", "Cuando sea"]
            ),
            CustomPoll(
                "¿Qué plan montamos para el finde?",
                ["Cena", "Cine", "Fiesta", "Maratón de series"]
            ),
            CustomPoll(
                "¿Qué pedimos para cenar?",
                ["Pizza", "Sushi", "Hamburguesas", "Tacos"],
                maxSelections: 2
            ),
        ];

    private static QuestionTemplate Superlative(string text, bool allowNobody = false) =>
        new()
        {
            Text = text,
            Type = QuestionType.Superlative,
            Metadata = new QuestionMetadata { AllowNobody = allowNobody, BlacklistedUserIds = [] },
        };

    private static QuestionTemplate Deathmatch(string text) =>
        new()
        {
            Text = text,
            Type = QuestionType.Deathmatch,
            Metadata = new QuestionMetadata { Teams = [] },
        };

    private static QuestionTemplate Scale(string text) =>
        new()
        {
            Text = text,
            Type = QuestionType.Scale,
            // TargetUserId resolved per-group at clone time.
            Metadata = new QuestionMetadata { RangeMin = 1, RangeMax = 10, TargetUserId = null },
        };

    private static QuestionTemplate SecretPairing(string text) =>
        new()
        {
            Text = text,
            Type = QuestionType.SecretPairing,
            Metadata = new QuestionMetadata { MinSelections = 2, MaxSelections = 2 },
        };

    private static QuestionTemplate CustomPoll(
        string text,
        string[] options,
        int minSelections = 1,
        int maxSelections = 1
    ) =>
        new()
        {
            Text = text,
            Type = QuestionType.CustomPoll,
            Metadata = new QuestionMetadata
            {
                MinSelections = minSelections,
                MaxSelections = maxSelections,
            },
            Options = [.. options.Select(o => new QuestionTemplateOption { Text = o })],
        };
}
