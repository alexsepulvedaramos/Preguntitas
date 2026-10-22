using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class SplitFlashAnonymity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAnonymous",
                table: "FlashEntries",
                newName: "AnonymousVotes");

            migrationBuilder.AddColumn<bool>(
                name: "AnonymousAuthor",
                table: "FlashEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // The single "anonymous" add-on of 1.30.0 hid the author and the votes alike.
            migrationBuilder.Sql("UPDATE \"FlashEntries\" SET \"AnonymousAuthor\" = \"AnonymousVotes\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymousAuthor",
                table: "FlashEntries");

            migrationBuilder.RenameColumn(
                name: "AnonymousVotes",
                table: "FlashEntries",
                newName: "IsAnonymous");
        }
    }
}
