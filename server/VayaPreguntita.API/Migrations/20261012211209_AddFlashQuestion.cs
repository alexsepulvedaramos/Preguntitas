using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FlashQuestion",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "FlashEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    LaunchedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashEntries_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashEntries_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashEntries_Users_LaunchedByUserId",
                        column: x => x.LaunchedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_GroupId_ClosesAt",
                table: "FlashEntries",
                columns: new[] { "GroupId", "ClosesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_GroupId_LaunchedByUserId_LocalDate",
                table: "FlashEntries",
                columns: new[] { "GroupId", "LaunchedByUserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_LaunchedByUserId",
                table: "FlashEntries",
                column: "LaunchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_QuestionId",
                table: "FlashEntries",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlashEntries");

            migrationBuilder.DropColumn(
                name: "FlashQuestion",
                table: "NotificationPreferences");
        }
    }
}
