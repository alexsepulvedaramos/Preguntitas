using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupFieldsCreatorAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminId",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AdminId",
                table: "Groups",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatorId",
                table: "Groups",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_AdminId",
                table: "Groups",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_CreatorId",
                table: "Groups",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_AdminId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CreatorId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_AdminId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CreatorId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Groups");
        }
    }
}
