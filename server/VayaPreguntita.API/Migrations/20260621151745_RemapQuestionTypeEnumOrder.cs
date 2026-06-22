using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class RemapQuestionTypeEnumOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QuestionType enum order changed from
            // (Superlative=0, Deathmatch=1, Scale=2, SecretPairing=3, CustomPoll=4) to
            // (CustomPoll=0, Superlative=1, Deathmatch=2, Scale=3, SecretPairing=4).
            // Remap already-persisted values so existing rows keep their original meaning.
            migrationBuilder.Sql(
                """
                UPDATE "Questions" SET "Type" = CASE "Type"
                    WHEN 0 THEN 1
                    WHEN 1 THEN 2
                    WHEN 2 THEN 3
                    WHEN 3 THEN 4
                    WHEN 4 THEN 0
                    ELSE "Type"
                END;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Questions" SET "Type" = CASE "Type"
                    WHEN 1 THEN 0
                    WHEN 2 THEN 1
                    WHEN 3 THEN 2
                    WHEN 4 THEN 3
                    WHEN 0 THEN 4
                    ELSE "Type"
                END;
                """
            );
        }
    }
}
