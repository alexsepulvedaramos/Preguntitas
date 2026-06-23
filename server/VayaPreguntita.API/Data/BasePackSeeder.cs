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
            Superlative("¿Quién enviaría una captura al grupo equivocado?"),
            Superlative("¿Quién llegaría tarde a su propio evento?"),
            Superlative("¿Quién daría la excusa más absurda para cancelar un plan?"),
            Superlative("¿Quién caería de nuevo con su ex tras jurar que no?"),
            Superlative("¿Quién discutiría con un desconocido por una tontería?"),
            Superlative("¿Quién olvida un aniversario pero recuerda chorradas?"),
            Superlative("¿Quién acabaría de íntimo amigo del DJ o del camarero?"),
            Superlative("¿Quién fundaría una secta totalmente por accidente?"),
            Superlative("¿Quién intentaría abrazar a un zombie en un apocalipsis?"),
            Superlative("¿Quién tendría un ataque de risa en el peor momento posible?"),
            // ---------- Deathmatch (teams auto-resolved at clone) ----------
            Deathmatch(
                "Batalla de gallos: ¿qué equipo humilla al rival soltando las mejores rimas?"
            ),
            Deathmatch("Escape Room: ¿qué equipo logra salir primero sin pedir ninguna pista?"),
            Deathmatch("Duelo de cocina con sobras: ¿qué equipo prepara algo comestible?"),
            Deathmatch("Competición de fuerza: ¿qué equipo gana en el tira y afloja?"),
            Deathmatch("Montar una rave clandestina: ¿qué equipo organiza el mejor evento?"),
            Deathmatch("Reconstrucción social: ¿qué equipo fundaría la civilización más justa?"),
            Deathmatch("Dilema ético: ¿qué equipo haría lo correcto sacrificando su éxito?"),
            Deathmatch("Poder absoluto: ¿qué bando mantendría sus principios sin corromperse?"),
            Deathmatch(
                "Negociación crítica: ¿qué equipo evitaría un conflicto usando solo la palabra?"
            ),
            Deathmatch(
                "Venta imposible: ¿qué equipo lograría venderle una idea absurda a un inversor?"
            ),
            // ---------- Scale (target auto-resolved at clone) ----------
            Scale("Del 1 al 10, ¿qué tan buen conductor te consideras?"),
            Scale("Del 1 al 10, ¿cómo sobrevivirías en la Edad Media?"),
            Scale("Del 1 al 10, ¿cuánto crees que el dinero compra la felicidad?"),
            Scale("Del 1 al 10, ¿cuánto confías en el futuro de la humanidad?"),
            Scale("Del 1 al 10, ¿cuán importante es dejar un legado al morir?"),
            Scale("Del 1 al 10, ¿cuánto te gusta la película Titanic?"),
            Scale("Del 1 al 10, ¿cómo puntúas el final de Juego de Tronos?"),
            Scale("Del 1 al 10, ¿qué nota le das a la pizza con piña?"),
            Scale("Del 1 al 10, ¿cuánto disfrutas las cenas de empresa?"),
            Scale("Del 1 al 10, ¿qué nota le pones a madrugar el fin de semana?"),
            // ---------- Secret Pairing ----------
            SecretPairing("¿Qué dos personas se perderían por negarse a mirar un mapa?"),
            SecretPairing("¿Qué dos personas sobrevivirían más tiempo perdidas en la naturaleza?"),
            SecretPairing("¿Qué dos personas debatirían durante horas sobre una tontería?"),
            SecretPairing("¿Qué dos personas montarían un podcast que nadie escucharía?"),
            SecretPairing("¿Qué dos personas ejecutarían el atraco perfecto a un banco?"),
            SecretPairing("¿Qué dos personas acabarían a gritos montando un mueble?"),
            SecretPairing("¿Qué dos personas compartirían piso y dejarían de hablarse?"),
            SecretPairing("¿Qué dos personas harían la dupla invencible en un torneo de mus?"),
            SecretPairing("¿Qué dos personas serían el policía bueno y malo en un interrogatorio?"),
            SecretPairing("¿Qué dos personas harían el mejor dúo cantando en un karaoke?"),
            // ---------- Custom Poll ----------
            CustomPoll("La eterna tortilla:", ["Con cebolla", "Sin cebolla"]),
            CustomPoll("¿Momento ideal para la ducha?", ["Por la mañana", "Por la noche"]),
            CustomPoll(
                "Elige tu superpoder inútil:",
                [
                    "Volar a ras de suelo",
                    "Invisibilidad a oscuras",
                    "Hablar con palomas",
                    "Saber la hora sin reloj",
                ]
            ),
            CustomPoll(
                "¿Qué plaga erradicarías hoy mismo?",
                ["Los mosquitos", "Los madrugones", "Audios largos", "La resaca"]
            ),
            CustomPoll(
                "Condena social, ¿qué prefieres?",
                ["Tener moco colgando", "Tener comida en dientes", "Sobacos siempre sudados"]
            ),
            CustomPoll(
                "¿Preferirías vivir con...?",
                [
                    "Decir siempre lo que piensas",
                    "Saber qué piensan de ti",
                    "Hablar en rimas",
                    "Solo gesticular",
                ]
            ),
            CustomPoll(
                "¿Viaje temporal?",
                ["Viajar 100 años al pasado", "Viajar 100 años al futuro"]
            ),
            CustomPoll(
                "Conocimiento oscuro:",
                ["Saber CÓMO vas a morir", "Saber CUÁNDO vas a morir"]
            ),
            CustomPoll(
                "Tortura doméstica:",
                ["Lavar siempre los platos", "Planchar siempre la ropa"]
            ),
            CustomPoll(
                "¿Qué prefieres perder?",
                ["No volver a comer dulce", "No volver a comer salado"]
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
            Metadata = new QuestionMetadata
            {
                RangeMin = 1,
                RangeMax = 10,
                TargetUserId = null,
            },
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
