using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class GuessTheChooser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorGuesses_Users_GuessedCreatorId",
                table: "AuthorGuesses");

            migrationBuilder.RenameColumn(
                name: "GuessedCreatorId",
                table: "AuthorGuesses",
                newName: "GuessedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorGuesses_GuessedCreatorId",
                table: "AuthorGuesses",
                newName: "IX_AuthorGuesses_GuessedUserId");

            migrationBuilder.AddColumn<bool>(
                name: "GuessLauncher",
                table: "FlashEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FlashGuesses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FlashEntryId = table.Column<int>(type: "integer", nullable: false),
                    GuessedUserId = table.Column<int>(type: "integer", nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    GuessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HintUsed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashGuesses", x => new { x.FlashEntryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_FlashGuesses_FlashEntries_FlashEntryId",
                        column: x => x.FlashEntryId,
                        principalTable: "FlashEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashGuesses_Users_GuessedUserId",
                        column: x => x.GuessedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlashGuesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashGuesses_GuessedUserId",
                table: "FlashGuesses",
                column: "GuessedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashGuesses_UserId",
                table: "FlashGuesses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorGuesses_Users_GuessedUserId",
                table: "AuthorGuesses",
                column: "GuessedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorGuesses_Users_GuessedUserId",
                table: "AuthorGuesses");

            migrationBuilder.DropTable(
                name: "FlashGuesses");

            migrationBuilder.DropColumn(
                name: "GuessLauncher",
                table: "FlashEntries");

            migrationBuilder.RenameColumn(
                name: "GuessedUserId",
                table: "AuthorGuesses",
                newName: "GuessedCreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_AuthorGuesses_GuessedUserId",
                table: "AuthorGuesses",
                newName: "IX_AuthorGuesses_GuessedCreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorGuesses_Users_GuessedCreatorId",
                table: "AuthorGuesses",
                column: "GuessedCreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
