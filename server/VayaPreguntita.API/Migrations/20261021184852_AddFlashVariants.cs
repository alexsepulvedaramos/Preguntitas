using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CapsuleOpenedAt",
                table: "FlashEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtendedByUserId",
                table: "FlashEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "FlashEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultsHiddenUntil",
                table: "FlashEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_ExtendedByUserId",
                table: "FlashEntries",
                column: "ExtendedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashEntries_ResultsHiddenUntil",
                table: "FlashEntries",
                column: "ResultsHiddenUntil",
                filter: "\"ResultsHiddenUntil\" IS NOT NULL AND \"CapsuleOpenedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashEntries_Users_ExtendedByUserId",
                table: "FlashEntries",
                column: "ExtendedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashEntries_Users_ExtendedByUserId",
                table: "FlashEntries");

            migrationBuilder.DropIndex(
                name: "IX_FlashEntries_ExtendedByUserId",
                table: "FlashEntries");

            migrationBuilder.DropIndex(
                name: "IX_FlashEntries_ResultsHiddenUntil",
                table: "FlashEntries");

            migrationBuilder.DropColumn(
                name: "CapsuleOpenedAt",
                table: "FlashEntries");

            migrationBuilder.DropColumn(
                name: "ExtendedByUserId",
                table: "FlashEntries");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "FlashEntries");

            migrationBuilder.DropColumn(
                name: "ResultsHiddenUntil",
                table: "FlashEntries");
        }
    }
}
