using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminToGroupMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add the new column (default false for all rows)
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "GroupMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Step 2: backfill — mark the current admin of each group
            migrationBuilder.Sql("""
                UPDATE "GroupMembers" gm
                SET "IsAdmin" = true
                FROM "Groups" g
                WHERE gm."GroupId" = g."Id"
                  AND gm."UserId"  = g."AdminId";
                """);

            // Step 3: remove the now-redundant AdminId FK and column
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_AdminId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_AdminId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "GroupMembers");

            migrationBuilder.AddColumn<int>(
                name: "AdminId",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AdminId",
                table: "Groups",
                column: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_AdminId",
                table: "Groups",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
