using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlacklistedUserIds",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MaxSelections",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Questions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Questions");

            migrationBuilder.AddColumn<List<int>>(
                name: "BlacklistedUserIds",
                table: "Questions",
                type: "integer[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxSelections",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxValue",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinValue",
                table: "Questions",
                type: "integer",
                nullable: true);
        }
    }
}
