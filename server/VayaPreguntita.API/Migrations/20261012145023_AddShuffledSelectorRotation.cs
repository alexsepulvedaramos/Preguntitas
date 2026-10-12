using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    // Shuffled selector rotation (§17.4): SelectorRound / LastSelectorRound, and
    // DailyEntry.IsAutoSelected (bool) → SelectionKind { Auto, Confirmed, Picked, Created }.
    public partial class AddShuffledSelectorRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectionKind",
                table: "DailyEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // true → Auto (0), false → Picked (2).
            migrationBuilder.Sql(
                @"UPDATE ""DailyEntries"" SET ""SelectionKind"" = CASE WHEN ""IsAutoSelected"" THEN 0 ELSE 2 END;");

            migrationBuilder.DropColumn(
                name: "IsAutoSelected",
                table: "DailyEntries");

            // Every existing group starts round 1 with everyone due a turn…
            migrationBuilder.AddColumn<int>(
                name: "SelectorRound",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LastSelectorRound",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // …except whoever already holds the pending turn, which counts as their round-1 turn.
            migrationBuilder.Sql(
                @"UPDATE ""GroupMembers"" gm SET ""LastSelectorRound"" = 1
                  FROM ""DailyEntries"" d
                  WHERE d.""ActivatedAt"" IS NULL
                    AND d.""GroupId"" = gm.""GroupId""
                    AND d.""SelectorUserId"" = gm.""UserId"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSelected",
                table: "DailyEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                @"UPDATE ""DailyEntries"" SET ""IsAutoSelected"" = (""SelectionKind"" = 0);");

            migrationBuilder.DropColumn(
                name: "SelectionKind",
                table: "DailyEntries");

            migrationBuilder.DropColumn(
                name: "SelectorRound",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "LastSelectorRound",
                table: "GroupMembers");
        }
    }
}
