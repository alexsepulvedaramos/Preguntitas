using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class VoteChangeNudgesAndRetireAccessories : Migration
    {
        // Rama 43 (§17.10, §17.15 item 7): "change my vote" and the toque / nudge, plus the
        // avatar accessories retired the same way as the badges (RetireBadgesAndShopTitles):
        // every owner gets the price back (a CosmeticRefund = 15 ledger row per item, RefId = the
        // item id), the items are removed and the equipped slot is dropped.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "RetiredCosmetics" ("Key" text PRIMARY KEY, "Id" int, "Price" int);
                INSERT INTO "RetiredCosmetics" VALUES
                    ('acc-sunglasses', 11, 200),
                    ('acc-party-hat', 12, 200),
                    ('acc-moustache', 13, 300),
                    ('acc-headphones', 14, 300),
                    ('acc-paper-crown', 15, 400),
                    ('acc-halo', 16, 500);

                INSERT INTO "CoinTransactions" ("UserId", "Amount", "Reason", "GroupId", "RefId", "CreatedAt")
                SELECT i."UserId", r."Price", 15, NULL, r."Id", now()
                FROM "UserItems" i JOIN "RetiredCosmetics" r ON r."Key" = i."ItemKey";

                UPDATE "Users" u SET "Coins" = u."Coins" + refund."Total"
                FROM (
                    SELECT i."UserId", SUM(r."Price") AS "Total"
                    FROM "UserItems" i JOIN "RetiredCosmetics" r ON r."Key" = i."ItemKey"
                    GROUP BY i."UserId"
                ) refund
                WHERE u."Id" = refund."UserId";

                DELETE FROM "UserItems" WHERE "ItemKey" IN (SELECT "Key" FROM "RetiredCosmetics");

                DROP TABLE "RetiredCosmetics";
                """);

            migrationBuilder.DropColumn(
                name: "AccessoryKey",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "Nudge",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Nudges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    TargetUserId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nudges_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nudges_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nudges_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nudges_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoteChanges",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteChanges", x => new { x.QuestionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_VoteChanges_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoteChanges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_GroupId_TargetUserId_SentAt",
                table: "Nudges",
                columns: new[] { "GroupId", "TargetUserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_QuestionId",
                table: "Nudges",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_SenderUserId",
                table: "Nudges",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Nudges_TargetUserId",
                table: "Nudges",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteChanges_UserId",
                table: "VoteChanges",
                column: "UserId");
        }

        // Brings the slot back empty; the refunds stay (the items are not re-granted).
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nudges");

            migrationBuilder.DropTable(
                name: "VoteChanges");

            migrationBuilder.DropColumn(
                name: "Nudge",
                table: "NotificationPreferences");

            migrationBuilder.AddColumn<string>(
                name: "AccessoryKey",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }
    }
}
