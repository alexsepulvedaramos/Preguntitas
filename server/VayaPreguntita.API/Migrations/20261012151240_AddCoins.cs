using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Coins",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FirstVoterUserId",
                table: "DailyEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoinTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    RefId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinTransactions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CoinTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Questions answered before this rama keep their real first voter, so an open
            // question doesn't hand the first-voter bonus to a later vote.
            migrationBuilder.Sql(
                """
                UPDATE "DailyEntries" d
                SET "FirstVoterUserId" = (
                    SELECT v."UserId" FROM "Votes" v
                    WHERE v."QuestionId" = d."QuestionId"
                    ORDER BY v."DateResponded", v."Id"
                    LIMIT 1)
                WHERE d."ActivatedAt" IS NOT NULL;
                """
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Coins_NonNegative",
                table: "Users",
                sql: "\"Coins\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_GroupId",
                table: "CoinTransactions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserId_CreatedAt",
                table: "CoinTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_UserId_Reason_RefId",
                table: "CoinTransactions",
                columns: new[] { "UserId", "Reason", "RefId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoinTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Coins_NonNegative",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Coins",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstVoterUserId",
                table: "DailyEntries");
        }
    }
}
