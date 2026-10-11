using Microsoft.EntityFrameworkCore.Migrations;
using VayaPreguntita.API.Enums;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <summary>
    /// Data-only: every Scale question is now rated 1–10 (§5.3). Votes on questions that had a
    /// custom range are rescaled proportionally first, so their results keep their meaning,
    /// then the stored bounds are rewritten. Not reversible — the original ranges are dropped.
    /// </summary>
    public partial class LockScaleRangeTo1To10 : Migration
    {
        private const int Scale = (int)QuestionType.Scale;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // round(1 + (v − min) × 9 / (max − min)), clamped in case a vote sat outside its range.
            migrationBuilder.Sql($$"""
                UPDATE "Votes" AS v
                SET "NumericValue" = LEAST(10, GREATEST(1, ROUND(
                    1 + (v."NumericValue" - (q."Metadata"->>'RangeMin')::int) * 9.0
                        / ((q."Metadata"->>'RangeMax')::int - (q."Metadata"->>'RangeMin')::int))))
                FROM "Questions" AS q
                WHERE v."QuestionId" = q."Id"
                  AND q."Type" = {{Scale}}
                  AND v."NumericValue" IS NOT NULL
                  AND (q."Metadata"->>'RangeMin') IS NOT NULL
                  AND (q."Metadata"->>'RangeMax') IS NOT NULL
                  AND (q."Metadata"->>'RangeMax')::int > (q."Metadata"->>'RangeMin')::int
                  AND ((q."Metadata"->>'RangeMin')::int <> 1 OR (q."Metadata"->>'RangeMax')::int <> 10);
                """);

            foreach (var table in new[] { "Questions", "QuestionTemplates" })
            {
                migrationBuilder.Sql($$"""
                    UPDATE "{{table}}"
                    SET "Metadata" = "Metadata" || '{"RangeMin": 1, "RangeMax": 10}'::jsonb
                    WHERE "Type" = {{Scale}}
                      AND ("Metadata"->>'RangeMin' IS DISTINCT FROM '1'
                           OR "Metadata"->>'RangeMax' IS DISTINCT FROM '10');
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: the original custom ranges and vote values are not kept.
        }
    }
}
