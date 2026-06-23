using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class DropVoteQuestionUserUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Votes_QuestionId_UserId",
                table: "Votes");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_QuestionId_UserId",
                table: "Votes",
                columns: new[] { "QuestionId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Votes_QuestionId_UserId",
                table: "Votes");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_QuestionId_UserId",
                table: "Votes",
                columns: new[] { "QuestionId", "UserId" },
                unique: true);
        }
    }
}
