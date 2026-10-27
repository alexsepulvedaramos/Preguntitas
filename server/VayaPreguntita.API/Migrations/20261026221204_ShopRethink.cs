using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <inheritdoc />
    public partial class ShopRethink : Migration
    {
        // Shop rethink (rama 57, §17.15 item 8):
        // - name colours and app themes are retired: every owner gets back what they paid (the
        //   purchase row; the list price if there is none) as a CosmeticRefund = 15 ledger row,
        //   RefId = the item id, and the items are removed;
        // - ring skins become finishes drawn over the tier's material: ids 2–4 now name Arcade,
        //   Ventisca and Llamarada, so their keys are renamed for anyone holding them;
        // - the seniority frame is retired: its wearers go back to the streak ring;
        // - plain frame colours are now sold in the shop: everyone gets the one they wear;
        // - seasonal frames come with a one-off push: a ShopNews preference (on) and the
        //   Announcements table that makes it go out once.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "RetiredCosmetics" ("Key" text PRIMARY KEY, "Id" int, "Price" int);
                INSERT INTO "RetiredCosmetics" VALUES
                    ('color-coral', 21, 250),
                    ('color-tangerine', 22, 250),
                    ('color-gold', 23, 250),
                    ('color-mint', 24, 250),
                    ('color-sky', 25, 250),
                    ('color-lavender', 26, 250),
                    ('color-pink', 27, 250),
                    ('color-lime', 28, 250),
                    ('theme-ocean', 61, 250),
                    ('theme-strawberry', 62, 250),
                    ('theme-grape', 63, 250),
                    ('theme-tangerine', 64, 250);

                CREATE TEMP TABLE "Refunds" AS
                SELECT i."UserId", r."Id", COALESCE(
                    (SELECT -t."Amount" FROM "CoinTransactions" t
                     WHERE t."UserId" = i."UserId" AND t."Reason" = 13 AND t."RefId" = r."Id"
                     LIMIT 1),
                    r."Price") AS "Amount"
                FROM "UserItems" i JOIN "RetiredCosmetics" r ON r."Key" = i."ItemKey";

                INSERT INTO "CoinTransactions" ("UserId", "Amount", "Reason", "GroupId", "RefId", "CreatedAt")
                SELECT "UserId", "Amount", 15, NULL, "Id", now() FROM "Refunds" WHERE "Amount" > 0;

                UPDATE "Users" u SET "Coins" = u."Coins" + refund."Total"
                FROM (SELECT "UserId", SUM("Amount") AS "Total" FROM "Refunds" WHERE "Amount" > 0 GROUP BY "UserId") refund
                WHERE u."Id" = refund."UserId";

                DELETE FROM "UserItems" WHERE "ItemKey" IN (SELECT "Key" FROM "RetiredCosmetics");

                DROP TABLE "Refunds";
                DROP TABLE "RetiredCosmetics";

                UPDATE "UserItems" SET "ItemKey" = CASE "ItemKey"
                    WHEN 'ring-pixel' THEN 'ring-arcade'
                    WHEN 'ring-ice' THEN 'ring-blizzard'
                    ELSE 'ring-flare' END
                WHERE "ItemKey" IN ('ring-pixel', 'ring-ice', 'ring-flames');
                UPDATE "Users" SET "RingSkinKey" = CASE "RingSkinKey"
                    WHEN 'ring-pixel' THEN 'ring-arcade'
                    WHEN 'ring-ice' THEN 'ring-blizzard'
                    ELSE 'ring-flare' END
                WHERE "RingSkinKey" IN ('ring-pixel', 'ring-ice', 'ring-flames');

                UPDATE "Users" SET "FrameColor" = 'streak' WHERE "FrameColor" = 'seniority';

                INSERT INTO "UserItems" ("UserId", "ItemKey", "PurchasedAt")
                SELECT "Id", 'frame-' || "FrameColor", now() FROM "Users"
                WHERE "FrameColor" IN ('purple', 'blue', 'green', 'yellow', 'orange', 'red', 'pink')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "NameColorKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ThemeKey",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "ShopNews",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Key);
                });
        }

        // Brings the columns back empty; refunds and gifted colours stay.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropColumn(
                name: "ShopNews",
                table: "NotificationPreferences");

            migrationBuilder.AddColumn<string>(
                name: "NameColorKey",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeKey",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }
    }
}
