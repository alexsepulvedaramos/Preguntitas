using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberStreaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HighestStreakEver",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SelectedTitleKey",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StreakFrameAutoApplied",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StreakDanger",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StreakDangerHoursBefore",
                table: "NotificationPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "BestStreak",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStreak",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastStreakEntryId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreakDangerNotifiedEntryId",
                table: "GroupMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Users without a chosen frame colour now follow their streak (rama 19).
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""FrameColor"" = 'streak' WHERE ""FrameColor"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // System chat messages have no author and can't survive UserId becoming required.
            migrationBuilder.Sql(@"DELETE FROM ""ChatMessages"" WHERE ""UserId"" IS NULL;");
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""FrameColor"" = NULL WHERE ""FrameColor"" IN ('streak', 'none');");

            migrationBuilder.DropColumn(
                name: "HighestStreakEver",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedTitleKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StreakFrameAutoApplied",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StreakDanger",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "StreakDangerHoursBefore",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "BestStreak",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CurrentStreak",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LastStreakEntryId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "StreakDangerNotifiedEntryId",
                table: "GroupMembers");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
