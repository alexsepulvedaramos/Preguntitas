using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VayaPreguntita.API.Data;

#nullable disable

namespace VayaPreguntita.API.Migrations
{
    /// <summary>
    /// Turns on Row Level Security for the three tables that were created without it, so the
    /// Supabase anon/authenticated roles (PostgREST) cannot read or write them. No policies are
    /// added on purpose: the API connects as the table owner, which bypasses RLS, exactly like
    /// every other table in the schema. No model change, so the snapshot is untouched.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261127090000_EnableRlsOnRemainingTables")]
    public partial class EnableRlsOnRemainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Announcements" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FlashGuesses" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "UserSeenMarks" ENABLE ROW LEVEL SECURITY;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Announcements" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "FlashGuesses" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "UserSeenMarks" DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}
