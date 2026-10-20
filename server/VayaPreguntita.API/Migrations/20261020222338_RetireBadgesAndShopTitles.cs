using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class RetireBadgesAndShopTitles : Migration
    {
        // Badge emojis and shop titles are retired (§17.15): every owner gets the price back
        // (a CosmeticRefund = 15 ledger row per item, RefId = the item id), the items are
        // removed, and anyone showing a shop title falls back to the automatic streak title.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "RetiredCosmetics" ("Key" text PRIMARY KEY, "Id" int, "Price" int);
                INSERT INTO "RetiredCosmetics" VALUES
                    ('badge-fire', 31, 150),
                    ('badge-eyes', 32, 150),
                    ('badge-popcorn', 33, 150),
                    ('badge-teapot', 34, 150),
                    ('badge-frog', 35, 150),
                    ('badge-coffee', 36, 150),
                    ('badge-chili', 37, 150),
                    ('badge-unicorn', 38, 150),
                    ('badge-nails', 39, 150),
                    ('badge-clown', 40, 150),
                    ('badge-goat', 41, 150),
                    ('badge-brain', 42, 150),
                    ('title-pro-gossip', 51, 200),
                    ('title-undecided', 52, 200),
                    ('title-devils-advocate', 53, 300),
                    ('title-oracle', 54, 400);

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

                UPDATE "Users" SET "SelectedTitleKey" = NULL WHERE "SelectedTitleKey" IN ('title-pro-gossip', 'title-undecided', 'title-devils-advocate', 'title-oracle');

                DROP TABLE "RetiredCosmetics";
                """);

            migrationBuilder.DropColumn(
                name: "BadgeKey",
                table: "Users");
        }

        // Brings the column back empty; the refunds stay (the items are not re-granted).
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeKey",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }
    }
}
