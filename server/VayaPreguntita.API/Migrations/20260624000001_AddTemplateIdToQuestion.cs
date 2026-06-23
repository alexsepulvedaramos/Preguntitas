using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateIdToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TemplateId",
                table: "Questions",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionTemplates_TemplateId",
                table: "Questions",
                column: "TemplateId",
                principalTable: "QuestionTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionTemplates_TemplateId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_TemplateId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Questions");
        }
    }
}
