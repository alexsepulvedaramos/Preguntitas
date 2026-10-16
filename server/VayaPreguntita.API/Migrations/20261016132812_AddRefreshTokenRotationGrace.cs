using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRotationGrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousTokenHash",
                table: "UserRefreshTokens",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_PreviousTokenHash",
                table: "UserRefreshTokens",
                column: "PreviousTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_PreviousTokenHash",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "PreviousTokenHash",
                table: "UserRefreshTokens");
        }
    }
}
