# Pack audit — October 2026

First content pass over the 307 seeded question templates, done on the `refactor/pack-catalog`
branch. The outcome lives in the catalog itself (`server/VayaPreguntita.API/Data/Packs/*.json`:
`"retired": true` entries and the reworded ones carrying `legacyText`); this note records *why*.
Mechanism: spec §6.8.

## Evidence and its limits

- **Only Carapene counts.** It is the one group where people actually play (14 members, 114
  questions, 26 Jun – 7 Oct 2026). The other groups are tests or answered only by the developer,
  so they were excluded from every figure.
- Only **31 of the 307 templates** have ever been played there, so per-template numbers are thin.
  Most verdicts are editorial; figures are quoted only where they exist.
- Participation = distinct voters ÷ members at activation. It fell month over month
  (72 % → 64 % → 61 % → 49 % → 39 %), which the developer attributes to fading novelty, fewer
  user-created questions and notifications that did not work — not to content. Everything below
  was therefore compared against the month's average, not in absolute terms.
- What worked in Carapene: concrete scenarios about the people in the group (survival, crime,
  skills), short "would you rather" with an everyday or absurd twist, short open answers
  ("Top 3 frutos secos": 13 of 13 voted). What did not: money, divorce/relationship exposure
  (the Citas pack is disabled there), abstract philosophy, long free-text stories.

## Criteria

1. **Duplicate** of another template → keep the copy in the pack where it fits best.
2. **Owner decision** → the developer asked for it explicitly.
3. Everything else flagged in the first proposal but *not* a duplicate and *not* named by the
   developer was **kept** (see below).

## Retired (43)

| Pack | Template | Reason |
|---|---|---|
| Base | ¿Quién caería de nuevo con su ex…? | Duplicate of the Citas one |
| Base | ¿Quién discutiría con un desconocido por una tontería? | Duplicate (Polémicas) |
| Base | ¿Quién tendría un ataque de risa en el peor momento? | Duplicate of Humor negro "reírse en un funeral" |
| Base | Reconstrucción social | Owner: abstract |
| Base | Tentación de ascenso | Owner: abstract |
| Base | Escala: confianza en el futuro de la humanidad | Owner |
| Base | Escala: importancia de dejar un legado | Owner |
| Base | Escala: pizza con piña | Duplicate (Comida) |
| Base | Escala: cenas de empresa | Owner: not everyone has them |
| Base | Escala: madrugar el fin de semana | Duplicate (Polémicas) |
| Base | Parejas: se perderían por no mirar un mapa | Duplicate of "perderse con el GPS" |
| Base | Parejas: debatirían horas por una tontería | Duplicate (Polémicas) |
| Base | Parejas: atraco perfecto a un banco | Duplicate of Crimen "dúo criminal" |
| Base | Parejas: a gritos montando un mueble | Owner (2 voters in Carapene) |
| Base | Parejas: torneo de mus | Owner: niche |
| Base | ¿Qué prefieres perder? (dulce/salado) | Duplicate (Comida) |
| Base | ¿Quién se gastaría el sueldo en un capricho? | Duplicate of Hipotéticos "gastar un millón" |
| Citas | ¿Qué duele más? (mensaje / ghosting) | Owner; duplicate of the ghosting poll |
| Comida | Marca lo que SÍ le pondrías a una pizza | Duplicate of "¿Piña en la pizza?" |
| Comida | Escala: tiquismiquis comiendo | Duplicate of the "¿Quién…?" version |
| Deportes | ¿Quién culpa siempre al árbitro? | Duplicate of "peor perdedor" |
| Dilemas | ¿Feliz engañado o infeliz consciente? | Duplicate of the experience-machine dilemma |
| Dilemas | Barco de Teseo | Owner: abstract (6 voters, 0 messages in Carapene) |
| Dilemas | El bien y el mal, ¿universales o culturales? | Owner: abstract |
| Dilemas | ¿Bueno o egoísta por naturaleza? | Owner: abstract |
| Dilemas | Intención o resultado | Owner: abstract |
| Dilemas | La paradoja de la tolerancia | Owner: abstract / political |
| Guarradas | ¿Qué te tragas? (uña / pelo) | Duplicate (bocata de uñas o pelos) |
| Guarradas | ¿Qué prefieres tener siempre? (pegajosa / mocos) | Duplicate (plato de mocos) |
| Guarradas | Un pelo en cada plato o una uña | Duplicate; lowest turnout of any pack question in Carapene (4 voters) |
| Hipotéticos | Trato de paciencia (1M ya / 10M en 10 años) | Duplicate (millón ya / 4.000 € al mes) |
| Hipotéticos | ¿Más tiempo libre o más dinero? | Duplicate of the working-life one |
| Humor negro | Canibalismo en los Andes | Owner: real tragedy |
| Humor negro | Comerse el uno al otro en un naufragio | Duplicate (hunger dilemma) |
| Reparto | Yoda, Mr. Bean, cerebro malvado, Hermione, Reina de Corazones, Mulan, Maléfica, Lisa Simpson | Duplicates of other characters in the same pack (8) |
| Supervivencia | ¿Quién sería el más inútil en el caos? | Duplicate of "el primero en palmarla" |

## Reworded in place (9)

| Pack | Before → after |
|---|---|
| Supervivencia | "…última esperanza para **repoblar la Tierra**" → "…para **reconstruir la civilización**" |
| Base | "¿Quién olvida un **aniversario**…?" → "…el **cumpleaños de un amigo**…" (no couples needed) |
| Deportes | "…torneo de **pádel**" → "…torneo de **dobles (pádel, tenis, ping-pong…)**" |
| Deportes | Two "Cuéntanos…" prompts → one-sentence answers |
| Confesiones | "…placer culpable más vergonzoso, sin filtro" → "…y en una frase" |
| Nostalgia | Two "Cuéntanos…" prompts → one-sentence answers |
| Humor negro | The "deshacerte de un cadáver" prompt, moved here from Crimen, now "en una frase" |

## Added (74)

Proposed question by question; the developer approved or amended each one. Placement follows how
the existing packs are built (every themed pack mixes types) rather than one pack per type:

| Pack | Added | What |
|---|---|---|
| **Qué prefieres: absurdos** (new) | 15 | Everyday-absurd "would you rather" polls; no gore, no money |
| **Talentos del grupo** (new) | 17 | 12 "¿Quién…?" skills, 2 duos (incl. "mejor dúo cómico", a Carapene hit), 3 self-rated scales |
| **Viajes y planes** (new) | 20 | 6 polls, 7 "¿Quién…?", 4 short open answers, 3 self-rated scales |
| Deportes y competencia | 8 | 6 team contests, 2 scales |
| Comida | 4 | Short open answers ("Top 3 snacks…", "restaurante chino") — Comida had none |
| Base | 6 | 3 generic open answers (Base had no open text) and 3 calendar-flavoured "¿Quién…?" |
| Confesiones y vergüenzas | 2 | Self-rated scales |
| Vida nocturna y resaca | 1 | Self-rated scale |
| Polémicas y bandos | 1 | "El turrón" poll |

Of 101 proposed, 27 were left out at the developer's call (e.g. the couch poll, singing when
ordering, carry-on vs suitcase, five of the team contests). Seasonal
questions (Halloween, Christmas…) were kept only where they read fine year-round, so no
date-window feature was needed.

## Kept on purpose

Not duplicates and not named by the developer, so they stay: the whole Citas pack (other audiences
may be mostly single — Carapene is almost all couples), "Trono vacío", "Rehén a punta de pistola",
the Titanic and Juego de Tronos scales, "compartirían piso y dejarían de hablarse", the Humor
negro death polls, "¿El fin justifica los medios?", "Hitler de bebé", "lo más caro que has
mangado", "los pesados de la clase", the debt/salary poll, and the grosser Guarradas
(Humor negro and Guarradas ship **disabled by default**).

## Decided along the way

- **Pack Scale questions never involve a member** (2026-10-08). The cloner used to attach a
  random member, so first-person texts rendered as "¿qué tan buen conductor te consideras? —
  Sobre Henar". Pack scales are self-assessments or plain opinions (spec §5.3, §6.3).
- Open text works when the answer is short; prompts now ask for one sentence.

## Still open

- Nothing from this pass. (`AllowOther` was being dropped when cloning, so the 11 polls meant to
  offer "Otro" never did; fixed separately in #52, existing clones are not backfilled.)
