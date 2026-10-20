using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSeenAndFrameKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "GroupMembers",
                type: "timestamp with time zone",
                nullable: true);

            // Frame colours become palette keys (§14): the seven hex values the old picker
            // offered map to their key; any other hex (never offered by the UI) falls back to
            // the streak ring.
            migrationBuilder.Sql("""
                UPDATE "Users" SET "FrameColor" = CASE lower("FrameColor")
                    WHEN '#8b5cf6' THEN 'purple'
                    WHEN '#3b82f6' THEN 'blue'
                    WHEN '#22c55e' THEN 'green'
                    WHEN '#eab308' THEN 'yellow'
                    WHEN '#f97316' THEN 'orange'
                    WHEN '#ef4444' THEN 'red'
                    WHEN '#ec4899' THEN 'pink'
                    ELSE 'streak'
                END
                WHERE "FrameColor" LIKE '#%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users" SET "FrameColor" = CASE "FrameColor"
                    WHEN 'purple' THEN '#8b5cf6'
                    WHEN 'blue' THEN '#3b82f6'
                    WHEN 'green' THEN '#22c55e'
                    WHEN 'yellow' THEN '#eab308'
                    WHEN 'orange' THEN '#f97316'
                    WHEN 'red' THEN '#ef4444'
                    WHEN 'pink' THEN '#ec4899'
                    ELSE "FrameColor"
                END;
                """);

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "GroupMembers");
        }
    }
}
