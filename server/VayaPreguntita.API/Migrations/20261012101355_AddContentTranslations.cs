using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddContentTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextTranslations",
                table: "QuestionTemplates",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TextTranslations",
                table: "QuestionTemplateOption",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TextTranslations",
                table: "Questions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionTranslations",
                table: "Packs",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "NameTranslations",
                table: "Packs",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TextTranslations",
                table: "Options",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "StreakDays",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreakUsername",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextTranslations",
                table: "QuestionTemplates");

            migrationBuilder.DropColumn(
                name: "TextTranslations",
                table: "QuestionTemplateOption");

            migrationBuilder.DropColumn(
                name: "TextTranslations",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DescriptionTranslations",
                table: "Packs");

            migrationBuilder.DropColumn(
                name: "NameTranslations",
                table: "Packs");

            migrationBuilder.DropColumn(
                name: "TextTranslations",
                table: "Options");

            migrationBuilder.DropColumn(
                name: "StreakDays",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "StreakUsername",
                table: "ChatMessages");
        }
    }
}
