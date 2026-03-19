using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NcaaBracket.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixBracketPositionsAndFirstFour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Allow round 0 for First Four games
            migrationBuilder.Sql(
                "ALTER TABLE games DROP CONSTRAINT IF EXISTS games_round_check;");
            migrationBuilder.Sql(
                "ALTER TABLE games ADD CONSTRAINT games_round_check CHECK (round BETWEEN 0 AND 6);");

            // Clear all games and let the ESPN sync rebuild with correct positions.
            // User picks are cleared too since game IDs will change.
            migrationBuilder.Sql("DELETE FROM user_picks;");
            migrationBuilder.Sql("DELETE FROM games;");
            migrationBuilder.Sql("DELETE FROM teams;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE games DROP CONSTRAINT IF EXISTS games_round_check;");
            migrationBuilder.Sql(
                "ALTER TABLE games ADD CONSTRAINT games_round_check CHECK (round BETWEEN 1 AND 6);");
        }
    }
}
