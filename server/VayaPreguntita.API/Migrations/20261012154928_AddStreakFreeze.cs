using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStreakFreeze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FreezeGiftFromUserId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FreezeUsedEntryId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastFreeFreezeMonth",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRepairMonth",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LostStreakEntryId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LostStreakValue",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreakFreezes",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreezeGiftFromUserId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "FreezeUsedEntryId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LastFreeFreezeMonth",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LastRepairMonth",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LostStreakEntryId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LostStreakValue",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "StreakFreezes",
                table: "GroupMembers");
        }
    }
}
