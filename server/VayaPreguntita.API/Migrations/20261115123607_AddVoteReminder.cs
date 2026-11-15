using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVoteReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The vote reminder (rama 61) is on for everyone, existing preference rows included.
            migrationBuilder.AddColumn<bool>(
                name: "VoteReminder",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "VoteReminderEntryId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoteReminder",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "VoteReminderEntryId",
                table: "GroupMembers");
        }
    }
}
