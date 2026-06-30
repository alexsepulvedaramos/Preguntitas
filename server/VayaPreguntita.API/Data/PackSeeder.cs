// Data/PackSeeder.cs
namespace VayaPreguntita.API.Data;

using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

// Idempotent runtime seeder for the global question packs (spec §6). Runs at startup.
//
// Idempotent *per template*, not per pack: each pack is found-or-created by Name, and each
// template is found-or-created by (PackId, Text). This lets content drops land safely on a
// database where a pack already exists, instead of being skipped by a whole-pack existence
// check. Existing templates are never edited or removed (that would orphan the
// Question.TemplateId FKs of clones already living in real groups) — additive only.
public static class PackSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        foreach (var def in BuildPacks())
        {
            var pack = await context.Packs.FirstOrDefaultAsync(p => p.Name == def.Name);

            if (pack is null)
            {
                pack = new Pack
                {
                    Name = def.Name,
                    Description = def.Description,
                    IsActiveByDefault = true,
                };
                context.Packs.Add(pack);
                await context.SaveChangesAsync();
            }
            else if (
                await context.QuestionTemplates.CountAsync(t => t.PackId == pack.Id)
                >= def.Templates.Count
            )
            {
                // Steady state: pack already fully seeded — skip without materializing any rows.
                continue;
            }

            var existingTexts = await context
                .QuestionTemplates.Where(t => t.PackId == pack.Id)
                .Select(t => t.Text)
                .ToHashSetAsync();

            foreach (var template in def.Templates.Where(t => !existingTexts.Contains(t.Text)))
            {
                template.PackId = pack.Id;
                context.QuestionTemplates.Add(template);
            }

            await context.SaveChangesAsync();
        }
    }

    private sealed record PackDef(string Name, string Description, List<QuestionTemplate> Templates);

    private static List<PackDef> BuildPacks() =>
        [
            new PackDef(
                "Base",
                "Preguntas base disponibles para todos los grupos.",
                BaseTemplates()
            ),
            new PackDef(
                "Humor negro",
                "Humor oscuro, morboso y gamberro. Solo para estómagos fuertes.",
                HumorNegro()
            ),
            new PackDef(
                "Supervivencia y apocalipsis",
                "Isla desierta, naufragios y fin del mundo. ¿Sobrevivirías?",
                Supervivencia()
            ),
            new PackDef(
                "Vida nocturna y resaca",
                "Fiesta, borracheras y resacas épicas.",
                VidaNocturna()
            ),
            new PackDef(
                "Dilemas",
                "Dilemas filosóficos y éticos sin respuesta correcta.",
                Dilemas()
            ),
            new PackDef(
                "Citas, relaciones y red flags",
                "Amor, ligues, parejas y banderas rojas.",
                Citas()
            ),
            new PackDef(
                "Crimen y misterio",
                "Crímenes perfectos, sospechosos y grandes misterios.",
                Crimen()
            ),
            new PackDef(
                "Comida",
                "Debates gastronómicos y manías de comer que separan amistades.",
                Comida()
            ),
            new PackDef(
                "Deportes y competencia",
                "Retos de equipo y piques competitivos.",
                Deportes()
            ),
            new PackDef(
                "Polémicas y bandos",
                "Temas intrascendentes que defenderás a muerte.",
                Polemicas()
            ),
            new PackDef(
                "Confesiones y vergüenzas",
                "Placeres culpables, manías y momentos cringe.",
                Confesiones()
            ),
            new PackDef(
                "Nostalgia y cringe",
                "Infancia, adolescencia y vergüenzas del pasado.",
                Nostalgia()
            ),
            new PackDef(
                "Hipotéticos: ¿qué harías?",
                "Superpoderes, golpes de suerte y trueques de vida.",
                Hipoteticos()
            ),
            new PackDef(
                "El reparto del grupo",
                "Reparte personajes icónicos entre los miembros del grupo.",
                Reparto()
            ),
            new PackDef(
                "Guarradas y dilemas asquerosos",
                "Disyuntivas asquerosas donde ninguna opción es buena.",
                Guarradas()
            ),
        ];

    // ──────────────────────────── Base (existing 50 + 12 additions) ────────────────────────────

    private static List<QuestionTemplate> BaseTemplates() =>
        [
            // ---------- Superlative ----------
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
            // ---------- Deathmatch ----------
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
            // ---------- Scale ----------
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
            // ---------- Additions (+12, skewed to people-centered) ----------
            Superlative("¿Quién sobreviviría más tiempo sin su móvil?"),
            Superlative("¿Quién se gastaría el sueldo entero en el primer capricho que viera?"),
            Superlative("¿Quién es más probable que se haga viral por accidente?"),
            Superlative("¿Quién se perdería incluso con el GPS en la mano?"),
            Superlative("¿Quién contaría un secreto que le has pedido guardar?"),
            Superlative("¿Quién daría la charla más motivadora aunque no tenga ni idea del tema?"),
            Deathmatch("Concurso de baile: ¿qué equipo se marca la mejor coreografía?"),
            Deathmatch("Montar una tienda de campaña: ¿qué equipo acaba antes y sin discutir?"),
            Deathmatch("Improvisar una obra de teatro: ¿qué equipo arranca más aplausos?"),
            SecretPairing("¿Qué dos personas montarían el negocio más absurdo y les iría bien?"),
            CustomPoll(
                "¿Qué superpoder cotidiano eliges?",
                [
                    "Encontrar siempre aparcamiento",
                    "Que el semáforo esté siempre en verde",
                    "No hacer nunca cola",
                    "Wifi perfecto en todas partes",
                ]
            ),
            CustomPoll(
                "¿Qué te fastidia más para dormir?",
                ["Una mosca zumbando en la habitación", "Una gotera haciendo \"ploc\" toda la noche"]
            ),
        ];

    // ──────────────────────────── Pack 1 — Humor negro (16) ────────────────────────────

    private static List<QuestionTemplate> HumorNegro() =>
        [
            CustomPoll(
                "Te obligan a elegir cómo palmarla. ¿Qué prefieres?",
                ["Quemado vivo", "Enterrado vivo", "Congelado poco a poco", "Devorado por tiburones"]
            ),
            CustomPoll(
                "Por 1 millón de euros, ¿qué estarías dispuesto a hacer?",
                [
                    "Pasar una noche entera en una morgue",
                    "No volver a hablar con tu mejor amigo",
                    "Donar el dedo meñique",
                    "Comer algo asqueroso a diario durante un año",
                ],
                maxSelections: 4
            ),
            CustomPoll(
                "Apocalipsis y hay hambre. ¿Cuál cruzas antes?",
                ["Comerte a tu mascota", "Comerte a tu vecino"]
            ),
            CustomPoll(
                "En tu funeral, ¿qué prefieres provocar?",
                ["Que todos lloren a mares", "Que todos se partan de risa recordándote"]
            ),
            CustomPoll(
                "Te toca un superpoder maldito. ¿Cuál aguantas?",
                [
                    "Resucitar siempre, pero con un dolor atroz cada vez",
                    "Ser inmortal viendo morir a todos los que quieres",
                ]
            ),
            CustomPoll(
                "¿Qué serías capaz de hacer con tal de no morir?",
                [
                    "Comer carne humana",
                    "Cortarte un brazo",
                    "Matar a un desconocido",
                    "Pasar el resto de tu vida en una celda",
                ],
                maxSelections: 4
            ),
            CustomPoll(
                "Si pudieras presenciar tu propia muerte como mero espectador (sin dolor), ¿lo harías?",
                ["Sí", "No"]
            ),
            Superlative("¿Quién recurriría al canibalismo primero si el avión se estrellara en los Andes?"),
            Superlative("¿Quién fingiría su propia muerte para librarse de un marrón?"),
            Superlative("¿A quién le darías un puñetazo por un millón de euros?"),
            Superlative("¿Quién se reiría sin poder parar en pleno funeral?"),
            Superlative("¿Quién sobreviviría más tiempo en La Purga?"),
            SecretPairing("¿Qué dos personas esconderían un cadáver juntas sin que les pillaran?"),
            SecretPairing("¿Qué dos personas se comerían la una a la otra primero en un naufragio?"),
            Scale("Del 1 al 10, ¿qué probabilidad hay de que acabes en el infierno?"),
            Deathmatch("Os toca limpiar la escena de un crimen. ¿Qué equipo no deja ni una prueba?"),
        ];

    // ──────────────────────────── Pack 2 — Supervivencia y apocalipsis (15) ────────────────────────────

    private static List<QuestionTemplate> Supervivencia() =>
        [
            Deathmatch("Naufragio: ¿qué equipo monta una balsa que de verdad flote?"),
            Superlative("¿Quién sería el primero en palmarla en un apocalipsis zombie?"),
            Superlative("¿Quién se autoproclamaría líder sin tener ni idea?"),
            Superlative("¿Quién entraría en pánico nada más empezar el caos?"),
            Superlative("¿Quién se quedaría dormido en su turno de guardia?"),
            Superlative(
                "En un grupo de supervivientes a un apocalipsis, ¿quién sería el más inútil cuando llegara el caos?"
            ),
            Superlative(
                "¿Quién se vendría arriba e intentaría una heroicidad que los pondría a todos en peligro?"
            ),
            SecretPairing("¿Qué dos personas montarían la alianza más letal para sobrevivir?"),
            SecretPairing(
                "Tras el fin del mundo, ¿qué dos personas serían la última esperanza para repoblar la Tierra?"
            ),
            CustomPoll(
                "Fin del mundo en 24h, ¿qué haces?",
                ["Fiesta sin freno", "Con la familia", "Saquear tiendas", "Dormir tranquilo"],
                allowOther: true
            ),
            CustomPoll(
                "Marca lo que NO aguantarías sin:",
                ["Café", "Móvil", "Ducha caliente", "Tu serie favorita"],
                maxSelections: 4
            ),
            CustomPoll(
                "Solo puedes coger un arma cuerpo a cuerpo para el apocalipsis zombie. ¿Cuál?",
                ["Bate de béisbol", "Katana", "Machete", "Sartén de hierro"]
            ),
            CustomPoll(
                "Montas tu kit de supervivencia y solo cabe una cosa más en la mochila. ¿Cuál metes?",
                ["Comida para una semana", "Un buen cuchillo", "Un botiquín", "Un mechero"]
            ),
            CustomPoll(
                "En un apocalipsis, ¿qué tipo de superviviente serías?",
                [
                    "El que acapara recursos",
                    "El que ayuda a todos",
                    "El llanero solitario",
                    "El que se esconde y reza",
                ]
            ),
            CustomPoll(
                "Si el mundo se fuera a acabar, ¿qué final prefieres?",
                ["Meteorito instantáneo", "Invasión zombie", "Pandemia lenta", "Guerra nuclear"],
                allowOther: true
            ),
        ];

    // ──────────────────────────── Pack 3 — Vida nocturna y resaca (16) ────────────────────────────

    private static List<QuestionTemplate> VidaNocturna() =>
        [
            Superlative("¿Quién es el primero en caer redondo de borracho?"),
            Superlative("¿Quién acaba bailando encima de una mesa?"),
            Superlative("¿Quién hace la \"bomba de humo\" y desaparece sin despedirse?"),
            Superlative("¿Quién manda los mensajes más vergonzosos a las 4 de la mañana?"),
            Superlative("¿Quién propone \"la última\" y acaban siendo las 7?"),
            Superlative("¿Quién se pone más moñas y cariñoso cuando bebe?"),
            Superlative("¿Quién tiene resacas de tres días y jura no beber nunca más?"),
            SecretPairing("¿Qué dos personas se irían de after hasta ver el amanecer?"),
            CustomPoll(
                "La cura milagrosa de la resaca:",
                [
                    "Dormir hasta las 3 de la tarde",
                    "Comida grasienta de rey",
                    "Seguir bebiendo (el clavo)",
                    "Sufrir en silencio",
                ],
                allowOther: true
            ),
            CustomPoll(
                "¿Qué resaca prefieres?",
                ["La física, el cuerpo hecho fosfatina", "La moral, la vergüenza de lo que hiciste"]
            ),
            CustomPoll(
                "A las 6 de la mañana eres:",
                ["El que grita \"¡una más!\"", "El que ya está pidiendo el taxi"]
            ),
            CustomPoll("Plan de viernes ideal:", ["Salir hasta reventar", "Sofá, manta y peli"]),
            CustomPoll(
                "La bebida que nunca te falla:",
                ["Cerveza", "Vino", "Combinados", "Chupitos"],
                allowOther: true
            ),
            CustomPoll(
                "Te despiertas con resacón y 20 mensajes sin leer. ¿Qué haces?",
                ["Leerlos de golpe y asumir el daño", "Apagar el móvil y vivir en negación"]
            ),
            Scale("Del 1 al 10, ¿cómo de fiestero eres en realidad?"),
            Deathmatch("Maratón de fiesta hasta el amanecer: ¿qué equipo aguanta en pie sin caer?"),
        ];

    // ──────────────────────────── Pack 4 — Dilemas (16) ────────────────────────────

    private static List<QuestionTemplate> Dilemas() =>
        [
            CustomPoll(
                "Te dan mal el cambio a tu favor (10€ de más) y te das cuenta. ¿Lo dices?",
                [
                    "Sí, siempre lo devuelvo",
                    "Depende: a una gran empresa me callo, a una tienda pequeña lo digo",
                    "Me callo y a ganar, sea quien sea",
                ]
            ),
            CustomPoll(
                "Puedes salvar a 5 desconocidos o a 1 ser querido. ¿A quién salvas?",
                ["A los 5 desconocidos", "A tu ser querido"]
            ),
            CustomPoll(
                "¿Mentirías a un amigo para no hacerle daño?",
                ["Sí, mentira piadosa", "No, la verdad por delante"]
            ),
            CustomPoll("¿El fin justifica los medios?", ["Sí, si el resultado es bueno", "No, nunca"]),
            CustomPoll(
                "¿Qué vida prefieres?",
                ["Corta pero intensa y que deje huella", "Larga y tranquila sin pena ni gloria"]
            ),
            CustomPoll(
                "¿Saber qué hay tras la muerte pero sin poder contarlo, o no saberlo jamás?",
                ["Saberlo en silencio", "No saberlo nunca"]
            ),
            CustomPoll(
                "¿Feliz viviendo engañado o infeliz pero sabiendo toda la verdad?",
                ["Feliz pero engañado", "Infeliz pero consciente"]
            ),
            CustomPoll(
                "Un tren va a atropellar a 5 personas. Puedes desviarlo a otra vía donde solo hay 1. ¿Tiras de la palanca?",
                ["Sí, desvío (muere 1, salvo 5)", "No intervengo (mueren 5)"]
            ),
            CustomPoll(
                "Para parar ese tren y salvar a 5, tendrías que empujar tú a un desconocido a la vía. ¿Lo empujas?",
                ["Sí", "No"]
            ),
            CustomPoll(
                "Te ofrecen enchufarte para siempre a una máquina que simula una vida perfecta, sin que sepas que es mentira. ¿Entras?",
                ["Sí, dame la vida perfecta", "No, prefiero la realidad aunque duela"]
            ),
            CustomPoll(
                "Si cambias una a una todas las piezas de un barco, ¿sigue siendo el mismo barco?",
                ["Sí, es el mismo", "No, ya es otro"]
            ),
            CustomPoll(
                "Viajas al pasado y tienes delante a Hitler de bebé. ¿Lo matas?",
                ["Sí", "No"]
            ),
            CustomPoll(
                "¿El bien y el mal son universales o los decide cada cultura?",
                ["Universales", "Relativos a cada cultura"]
            ),
            CustomPoll(
                "¿El ser humano es bueno por naturaleza o egoísta por naturaleza?",
                ["Bueno por naturaleza", "Egoísta por naturaleza"]
            ),
            CustomPoll("Al juzgar un acto, ¿qué importa más?", ["La intención", "El resultado"]),
            CustomPoll(
                "La paradoja de la tolerancia: ¿hay que tolerar a los intolerantes?",
                ["Sí, a todos", "No, a los intolerantes no"]
            ),
        ];

    // ──────────────────────────── Pack 5 — Citas, relaciones y red flags (20) ────────────────────────────

    private static List<QuestionTemplate> Citas() =>
        [
            SecretPairing("¿Qué dos personas del grupo harían la mejor pareja?"),
            SecretPairing("¿Qué dos personas acabarían liándose en una boda?"),
            SecretPairing("¿Qué dos personas serían la pareja más tóxica?"),
            SecretPairing("¿Qué dos personas ligan fatal por separado pero juntas serían perfectas?"),
            SecretPairing(
                "¿Qué dos personas seguro que se han mandado un mensajito subido de tono alguna vez?"
            ),
            SecretPairing("¿Qué dos personas discutirían a diario pero no podrían vivir la una sin la otra?"),
            SecretPairing("¿Qué dos personas tendrían el romance de verano más intenso?"),
            Superlative("¿Quién volvería con su ex por enésima vez jurando que esta vez sí?"),
            Superlative("¿Quién tiene el peor gusto eligiendo con quién sale?"),
            Superlative("¿Quién stalkea más a su crush en redes?"),
            Superlative("¿Quién ligaría más en una noche de fiesta?"),
            Superlative("¿Quién se pillaría a las dos semanas de conocer a alguien?"),
            Superlative("¿Quién sería el más celoso en una relación?"),
            CustomPoll(
                "La red flag más imperdonable en una primera cita:",
                [
                    "Llega 40 min tarde sin avisar",
                    "Habla solo de su ex",
                    "Es borde con el camarero",
                    "No suelta el móvil",
                ],
                allowOther: true
            ),
            CustomPoll(
                "¿Qué prefieres vivir?",
                ["Una mala cita y que no pase de ahí", "Tres citas geniales y que acabe en ghosting"]
            ),
            CustomPoll("¿Quién crea tu próximo perfil de citas?", ["Tu madre", "Tu ex"]),
            CustomPoll(
                "¿Con quién sales?",
                ["Alguien que odia a los animales", "Alguien con siete perros y que los antepone a todo"]
            ),
            CustomPoll(
                "¿Qué duele más?",
                ["Que te dejen por un mensaje", "Que te hagan ghosting sin explicación"]
            ),
            CustomPoll(
                "El green flag definitivo:",
                [
                    "Trata bien al camarero",
                    "Se lleva genial con sus amigos",
                    "Contesta rápido los mensajes",
                    "Le va bien estando solo",
                ],
                allowOther: true
            ),
            CustomPoll("En una ruptura, ¿qué prefieres?", ["Dejar tú", "Que te dejen a ti"]),
        ];

    // ──────────────────────────── Pack 6 — Crimen y misterio (15) ────────────────────────────

    private static List<QuestionTemplate> Crimen() =>
        [
            Superlative("¿Quién cometería el crimen perfecto y no lo pillarían jamás?"),
            Superlative("¿Quién confesaría entre lágrimas al primer interrogatorio?"),
            Superlative("¿Quién sería el cerebro de una banda criminal?"),
            Superlative("¿Quién acabaría detenido por la tontería más absurda?"),
            Superlative("¿Quién parece el más inocente pero esconde el lado más oscuro?"),
            Superlative("¿A quién acusarían primero si apareciera un cadáver en una cena del grupo?"),
            Superlative("¿Quién se libraría de una multa hablándole bien al policía?"),
            SecretPairing("¿Qué dos personas formarían el dúo criminal más temido?"),
            SecretPairing("¿Qué dos personas se delatarían mutuamente en cuanto les apretaran un poco?"),
            CustomPoll(
                "¿Qué clase de criminal serías?",
                ["El cerebro", "El músculo", "El de los contactos", "El que conduce y poco más"]
            ),
            CustomPoll(
                "Si tuvieras que cometer un crimen y salir impune, ¿cuál?",
                [
                    "Atraco a mano armada a un banco",
                    "Hackeo millonario desde el sofá",
                    "Robo de una obra de arte en un museo",
                    "Una estafa piramidal de las gordas",
                ]
            ),
            CustomPoll(
                "¿Qué gran misterio te gustaría resolver de verdad?",
                [
                    "Quién mató a JFK",
                    "Qué hay en el Triángulo de las Bermudas",
                    "Si estamos solos en el universo",
                    "Qué pasó de verdad con el avión MH370",
                ]
            ),
            Scale("Del 1 al 10, ¿cómo de bien mientes bajo presión?"),
            OpenText("Confiesa: ¿qué es lo más caro que has mangado alguna vez y de dónde?"),
            OpenText("En teoría, eh... ¿cuál sería la mejor manera de deshacerte de un cadáver?"),
        ];

    // ──────────────────────────── Pack 8 — Comida (17) ────────────────────────────

    private static List<QuestionTemplate> Comida() =>
        [
            CustomPoll("¿Piña en la pizza?", ["Sí, y está buenísima", "Un crimen contra la humanidad"]),
            CustomPoll("¿Un hotdog es un sándwich?", ["Sí, pan + relleno = sándwich", "Ni de coña"]),
            CustomPoll(
                "¿Desayunar pizza fría del día anterior?",
                ["Manjar de campeones", "Aberración"]
            ),
            CustomPoll("Si solo pudieras comer un sabor el resto de tu vida:", ["Dulce", "Salado"]),
            CustomPoll(
                "¿Hay que terminar siempre todo el plato?",
                ["Sí, no se tira la comida", "Paro cuando estoy lleno"]
            ),
            CustomPoll(
                "El cereal con leche:",
                ["Primero leche, luego cereal", "Primero cereal, luego leche"]
            ),
            CustomPoll(
                "Marca lo que SÍ le pondrías a una pizza aunque escandalice:",
                ["Piña", "Huevo", "Patatas fritas", "Kebab"],
                maxSelections: 4
            ),
            CustomPoll(
                "¿Tu condimento de cabecera para echarle a todo?",
                ["Kétchup", "Mostaza", "Mayonesa"],
                allowOther: true
            ),
            CustomPoll("¿Kétchup en la tortilla de patatas?", ["Está de vicio", "Es un atentado"]),
            CustomPoll(
                "¿Cómo pides el chuletón?",
                ["Que casi haga \"mu\"", "En su punto, jugoso", "Hecho pero tierno", "Como la suela de un zapato"]
            ),
            CustomPoll(
                "Marca los crímenes culinarios que has cometido:",
                [
                    "Kétchup a la paella",
                    "Pedir la carne como una suela",
                    "Partir los espaguetis antes de cocer",
                    "Pizza con cuchillo y tenedor",
                ],
                maxSelections: 4,
                allowOther: true
            ),
            Superlative("¿Quién come más rápido, casi sin masticar?"),
            Superlative("¿Quién es el más tiquismiquis para comer?"),
            Superlative("¿Quién te robaría comida del plato sin pedir permiso?"),
            Superlative("¿Quién mezcla comidas raras que dan grima?"),
            SecretPairing("¿Qué dos personas se pelearían por el último trozo de pizza?"),
            Scale("Del 1 al 10, ¿cómo de tiquismiquis eres comiendo?"),
        ];

    // ──────────────────────────── Pack 9 — Deportes y competencia (18) ────────────────────────────

    private static List<QuestionTemplate> Deportes() =>
        [
            Deathmatch("Partido de fútbol a muerte: ¿qué equipo gana?"),
            Deathmatch("Gymkana imposible por la ciudad: ¿qué equipo llega primero a la meta?"),
            Deathmatch("Concurso de cultura general: ¿qué equipo sabe más?"),
            Deathmatch("Búsqueda del tesoro: ¿qué equipo lo encuentra antes?"),
            Superlative("¿Quién es el peor perdedor del grupo?"),
            Superlative("¿Quién hace trampas en cuanto te despistas?"),
            Superlative("¿Quién se pone competitivo hasta en el Parchís?"),
            Superlative("¿Quién celebra una victoria como si hubiera ganado el Mundial?"),
            Superlative("¿Quién culpa siempre al árbitro o a la mala suerte cuando pierde?"),
            Superlative("¿Quién es el más patoso para cualquier deporte?"),
            SecretPairing("¿Qué dos personas formarían la pareja imbatible en un torneo de pádel?"),
            SecretPairing("¿Qué dos personas acabarían enfadadas por un simple partido amistoso?"),
            CustomPoll(
                "¿Prefieres ganar haciendo trampas o perder limpiamente?",
                ["Ganar tramposo", "Perder limpio"]
            ),
            CustomPoll(
                "¿Qué prefieres ser?",
                ["El mejor de un equipo malísimo", "El peor de un equipo campeón"]
            ),
            CustomPoll(
                "En un juego de mesa, ¿qué eres?",
                ["El estratega", "El tramposo", "El que se enfada", "El que va a su bola"]
            ),
            Scale("Del 1 al 10, ¿cómo de competitivo eres en realidad?"),
            OpenText("Cuéntanos tu mayor momento de gloria deportiva."),
            OpenText("Cuéntanos tu momento más patético haciendo deporte."),
        ];

    // ──────────────────────────── Pack 10 — Polémicas y bandos (16) ────────────────────────────

    private static List<QuestionTemplate> Polemicas() =>
        [
            CustomPoll(
                "El papel higiénico se coloca…",
                ["Con la hoja por delante", "Con la hoja por detrás"]
            ),
            CustomPoll(
                "Marca TODAS las posturas que defenderías a muerte:",
                [
                    "El finde empieza el viernes",
                    "El verano es mejor que el invierno",
                    "El café sin azúcar está mejor",
                    "Madrugar el finde es de locos",
                ],
                maxSelections: 4
            ),
            CustomPoll(
                "La peor de estas costumbres ajenas es:",
                [
                    "Hablar en el cine",
                    "Masticar con la boca abierta",
                    "Poner el manos libres en público",
                    "Dejar el carrito en mitad del súper",
                ],
                allowOther: true
            ),
            CustomPoll("Para vacacionar de verdad:", ["Playa", "Montaña"]),
            CustomPoll(
                "Para ver una serie o peli extranjera:",
                ["Versión original subtitulada", "Doblada a tu idioma"]
            ),
            CustomPoll(
                "En una casa compartida, la tapa del váter:",
                ["Siempre bajada", "Da igual, se deja como esté"]
            ),
            CustomPoll("A la hora de dormir:", ["Con calcetines", "Jamás con calcetines"]),
            CustomPoll(
                "Nada más levantarte:",
                ["Hago la cama siempre", "¿Para qué, si me vuelvo a meter?"]
            ),
            CustomPoll(
                "Mandar audios de 3 minutos por WhatsApp:",
                ["Cómodo y normal", "Una falta de respeto"]
            ),
            CustomPoll(
                "En el avión, reclinar tu asiento hacia atrás:",
                ["Para eso está, es tu derecho", "Es de egoístas"]
            ),
            CustomPoll(
                "Sandalias con calcetines:",
                ["Comodidad ante todo", "Crimen de moda imperdonable"]
            ),
            CustomPoll(
                "Llegar a una fiesta en casa de alguien:",
                ["A la hora exacta que dijeron", "15-30 min tarde es lo correcto"]
            ),
            CustomPoll(
                "El doble check azul de WhatsApp:",
                ["Activado, no escondo nada", "Desactivado, mi vida es mía"]
            ),
            CustomPoll(
                "¿Cada cuánto lavas la toalla de baño?",
                ["Aguanta semanas, total te secas limpio", "Cada pocos usos, por higiene"]
            ),
            Superlative(
                "¿Quién es más capaz de discutir una hora por una tontería sin dar su brazo a torcer?"
            ),
            Superlative("¿Quién defiende las opiniones más impopulares solo por llevar la contraria?"),
        ];

    // ──────────────────────────── Pack 11 — Confesiones y vergüenzas (16) ────────────────────────────

    private static List<QuestionTemplate> Confesiones() =>
        [
            Superlative("¿Quién tarda tres días en contestar un mensaje?"),
            Superlative("¿Quién ha llorado con un anuncio de la tele?"),
            Superlative("¿Quién se hace el ocupado para no saludar a alguien por la calle?"),
            Superlative("¿Quién relee sus propios mensajes riéndose de lo gracioso que es?"),
            Superlative("¿Quién finge haber leído libros o visto pelis que no ha tocado?"),
            Superlative("¿Quién tiene el historial de Spotify más vergonzoso?"),
            CustomPoll("Yo nunca he cantado a grito pelado pensando que no me oía nadie", ["Sí", "No"]),
            CustomPoll("Yo nunca he olido la ropa para decidir si ponérmela otra vez", ["Sí", "No"]),
            CustomPoll(
                "Yo nunca he respondido \"tú también\" a un \"que aproveche\" o \"feliz cumpleaños\"",
                ["Sí", "No"]
            ),
            CustomPoll("Yo nunca he fingido estar enfermo para librarme de un plan", ["Sí", "No"]),
            CustomPoll(
                "Marca tus placeres culpables:",
                [
                    "Realities malísimos",
                    "Canciones de Abraham Mateo o Justin Bieber",
                    "Cotillear perfiles a las 3am",
                    "Comer de pie sobre el fregadero",
                ],
                maxSelections: 4
            ),
            CustomPoll(
                "Marca lo que has hecho alguna vez:",
                [
                    "Mirar el móvil 30 min en el baño",
                    "Aplicar la regla de los 5 segundos",
                    "Fingir una llamada para escapar de alguien",
                    "Hablar solo en voz alta",
                ],
                maxSelections: 4
            ),
            CustomPoll(
                "Confesión rápida: en los planes eres…",
                [
                    "El que siempre llega tarde",
                    "El que nunca contesta",
                    "El que desaparece sin avisar",
                    "El que cancela en el último momento",
                ],
                allowOther: true
            ),
            CustomPoll(
                "¿Qué te da MÁS vergüenza que te pillen haciendo?",
                ["Hablando solo", "Bailando frente al espejo", "Llorando con una peli", "Cantando en la ducha"]
            ),
            Scale("Del 1 al 10, ¿cómo de cotilla eres en realidad?"),
            OpenText("Confiesa tu placer culpable más vergonzoso, sin filtro."),
        ];

    // ──────────────────────────── Pack 12 — Nostalgia y cringe (15) ────────────────────────────

    private static List<QuestionTemplate> Nostalgia() =>
        [
            Superlative("¿Quién tuvo la fase más vergonzosa en la adolescencia?"),
            Superlative("¿Quién era el típico empollón del colegio?"),
            Superlative("¿Quién era el más gamberro de clase?"),
            Superlative("¿Quién tenía el corte de pelo más cuestionable de joven?"),
            Superlative("¿Quién ha pegado el mayor cambio (glow up) con los años?"),
            CustomPoll(
                "¿Qué época era mejor?",
                ["Cuando no había móviles", "Ahora con todo a un clic"]
            ),
            CustomPoll(
                "Si pudieras volver a una etapa, ¿a cuál?",
                [
                    "La infancia sin preocupaciones",
                    "La adolescencia rebelde",
                    "Los años locos de fiesta",
                    "Ninguna, mejor el presente",
                ]
            ),
            CustomPoll(
                "¿Qué reliquia tecnológica echas de menos?",
                [
                    "La videoconsola de tu infancia",
                    "El móvil de tapa y teclas",
                    "Los CDs y casetes",
                    "El MSN Messenger",
                ],
                allowOther: true
            ),
            CustomPoll(
                "¿Qué recuerdas con más cariño de pequeño?",
                [
                    "Los dibujos del sábado por la mañana",
                    "Jugar en la calle hasta que anochecía",
                    "Las meriendas en casa de un amigo",
                    "Los veranos que no acababan nunca",
                ]
            ),
            CustomPoll(
                "Marca tu(s) tribu(s) de adolescente:",
                [
                    "Pijo",
                    "Friki",
                    "Rebelde/macarra",
                    "Hippie/alternativo",
                    "Skater",
                    "El normal que pasaba desapercibido",
                ],
                maxSelections: 6
            ),
            SecretPairing("¿Qué dos personas habrían sido los pesados de la clase juntos?"),
            Scale("Del 1 al 10, ¿cómo de cringe era tu yo adolescente?"),
            OpenText("Cuéntanos tu momento más cringe de la adolescencia."),
            OpenText("¿Cuál era tu grupo o canción favorita de joven que ahora te da vergüenza?"),
            OpenText("¿Cuál era tu sueño de pequeño que ahora te da ternura (o risa)?"),
        ];

    // ──────────────────────────── Pack 13 — Hipotéticos: ¿qué harías? (16) ────────────────────────────

    private static List<QuestionTemplate> Hipoteticos() =>
        [
            CustomPoll("El superpoder definitivo:", ["Volar", "Ser invisible"]),
            CustomPoll(
                "Si pudieras elegir un poder:",
                ["Teletransportarte a cualquier sitio", "Viajar en el tiempo"]
            ),
            CustomPoll(
                "¿Cómo prefieres el dinero?",
                ["1 millón de golpe ahora mismo", "4.000 € cada mes el resto de tu vida"]
            ),
            CustomPoll(
                "Trato de paciencia:",
                ["1 millón mañana", "10 millones dentro de 10 años"]
            ),
            CustomPoll(
                "Tu vida laboral ideal:",
                ["Forrado haciendo algo que odias", "Justo a fin de mes amando lo que haces"]
            ),
            CustomPoll(
                "¿Qué vida prefieres? (sano y en forma todo el tiempo)",
                ["Una sola vida de 800 años", "8 vidas de 100 años"]
            ),
            CustomPoll("Te dan un mando para tu vida. ¿Cuál?", ["Botón de pausa", "Botón de rebobinar"]),
            CustomPoll(
                "Te toca la eternidad en uno. ¿Cuál eliges?",
                [
                    "Un cielo perfecto pero aburridísimo para siempre",
                    "Un infierno entretenido pero con dolor constante",
                ]
            ),
            CustomPoll(
                "Te reencarnas en lo que peor te caiga. ¿Qué prefieres ser?",
                ["Cucaracha indestructible", "Mosquito que vive un día molestando", "Pez con 3 s de memoria"]
            ),
            CustomPoll("¿Qué prefieres tener?", ["Más tiempo libre", "Más dinero"]),
            CustomPoll(
                "Pacto con el universo:",
                ["Cancelar todas tus deudas hoy", "Duplicar tu sueldo para siempre"]
            ),
            CustomPoll(
                "Una llamada imposible:",
                ["1 minuto con tu yo del pasado", "1 minuto con tu yo del futuro"]
            ),
            CustomPoll(
                "Si tu cuerpo no necesitara una cosa:",
                ["No volver a dormir nunca", "No volver a comer nunca"]
            ),
            Superlative("¿Quién usaría sus superpoderes para el mal sin pensárselo?"),
            Superlative("¿Quién se gastaría un millón de euros en menos de un mes?"),
            Superlative("¿Quién malgastaría un deseo mágico en una absoluta chorrada?"),
        ];

    // ──────────────────────────── Pack 14 — El reparto del grupo (34) ────────────────────────────

    private static List<QuestionTemplate> Reparto() =>
        [
            Superlative("¿Quién del grupo sería el Joker (puro caos)?"),
            Superlative("¿Quién sería Batman (oscuro y va por libre)?"),
            Superlative("¿Quién sería Homer Simpson (un desastre adorable)?"),
            Superlative("¿Quién sería Gandalf (el sabio que guía a todos)?"),
            Superlative("¿Quién sería Yoda (suelta perlas de sabiduría rara)?"),
            Superlative("¿Quién sería James Bond (elegante y ligón)?"),
            Superlative("¿Quién sería Sherlock Holmes (lo deduce todo)?"),
            Superlative("¿Quién sería Mr. Bean (el desastre silencioso)?"),
            Superlative("¿Quién sería el Grinch (odia la diversión)?"),
            Superlative("¿Quién sería Peter Pan (se niega a madurar)?"),
            Superlative("¿Quién sería el héroe que salva el día?"),
            Superlative("¿Quién sería el villano de la película?"),
            Superlative("¿Quién sería el alivio cómico del grupo?"),
            Superlative("¿Quién sería el que muere en el primer capítulo?"),
            Superlative("¿Quién sería el villano secreto que nadie ve venir?"),
            Superlative("¿Quién sería el protagonista absoluto de la serie?"),
            Superlative("¿Quién sería el secundario que se roba todas las escenas?"),
            Superlative("¿Quién sería el cerebro malvado detrás de todo?"),
            Superlative("¿Quién sería Wonder Woman (la heroína que puede con todo)?"),
            Superlative("¿Quién sería Hermione Granger (la lista que se lo sabe todo)?"),
            Superlative("¿Quién sería Cruella de Vil (malvada con muchísimo estilo)?"),
            Superlative("¿Quién sería Mary Poppins (perfecta y lo arregla todo)?"),
            Superlative("¿Quién sería Lara Croft (la aventurera intrépida)?"),
            Superlative("¿Quién sería Catwoman (felina y va a su rollo)?"),
            Superlative("¿Quién sería la Reina de Corazones (manda y mete miedo)?"),
            Superlative("¿Quién sería Campanilla (pequeña pero con un genio de cuidado)?"),
            Superlative("¿Quién sería Elsa (distante pero con muchísimo poder)?"),
            Superlative("¿Quién sería Mulan (guerrera que rompe todas las reglas)?"),
            Superlative("¿Quién sería la princesa Leia (líder y rebelde)?"),
            Superlative("¿Quién sería Maléfica (villana elegante y temible)?"),
            Superlative("¿Quién sería Lisa Simpson (la lista que nadie escucha)?"),
            Superlative("¿Quién sería Marge Simpson (la que aguanta a todos con paciencia)?"),
            Superlative("¿Quién sería la reina del drama (la diva total del grupo)?"),
            Superlative("¿Quién sería Matilda (lista y con poderes ocultos)?"),
        ];

    // ──────────────────────────── Pack 15 — Guarradas y dilemas asquerosos (15) ────────────────────────────

    private static List<QuestionTemplate> Guarradas() =>
        [
            CustomPoll("¿Qué prefieres?", ["Sudar mayonesa", "Llorar kétchup"]),
            CustomPoll("¿Qué lames?", ["El suelo del metro", "El pasamanos de una escalera mecánica"]),
            CustomPoll("¿Qué prefieres tener?", ["Dedos de salchicha", "Pelo de espagueti"]),
            CustomPoll("¿Beberte de un trago un vaso de aceite o uno de vinagre?", ["Aceite", "Vinagre"]),
            CustomPoll("¿Qué prefieres que te hagan encima?", ["Que te caguen", "Que te vomiten"]),
            CustomPoll(
                "¿Con qué cargas para siempre?",
                ["Mal aliento permanente", "Sudar a chorros sin parar"]
            ),
            CustomPoll("¿Qué te tragas?", ["Una uña del pie", "Un mechón de pelo"]),
            CustomPoll("¿Qué bocata te comes?", ["De uñas", "De pelos"]),
            CustomPoll("¿Qué bebes?", ["El agua de fregar los platos", "El agua de una bañera ajena"]),
            CustomPoll("¿Qué prefieres tener siempre?", ["La piel pegajosa", "Mocos colgando"]),
            CustomPoll(
                "¿Beber leche caducada en grumos o comer huevos podridos?",
                ["Leche en grumos", "Huevos podridos"]
            ),
            CustomPoll(
                "¿Limpiarte con papel higiénico ya usado o no usar nada?",
                ["Papel usado", "Nada"]
            ),
            CustomPoll(
                "¿Que se te meta una cucaracha en la oreja o una araña por la nariz?",
                ["Cucaracha en la oreja", "Araña por la nariz"]
            ),
            CustomPoll(
                "¿Comer un plato de mocos o beber un vaso de babas?",
                ["Plato de mocos", "Vaso de babas"]
            ),
            CustomPoll(
                "¿Encontrarte siempre un pelo en cada plato o una uña de vez en cuando?",
                ["Un pelo siempre", "Una uña a veces"]
            ),
        ];

    // ──────────────────────────── Template helpers ────────────────────────────

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
            // TargetUserId resolved per-group at clone time when set; null = subjective topic.
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
        int maxSelections = 1,
        bool allowOther = false
    ) =>
        new()
        {
            Text = text,
            Type = QuestionType.CustomPoll,
            Metadata = new QuestionMetadata
            {
                MinSelections = minSelections,
                MaxSelections = maxSelections,
                AllowOther = allowOther,
            },
            Options = [.. options.Select(o => new QuestionTemplateOption { Text = o })],
        };

    private static QuestionTemplate OpenText(string text) =>
        new()
        {
            Text = text,
            Type = QuestionType.OpenText,
            Metadata = new QuestionMetadata(),
        };
}
