using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NcaaBracket.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLockDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE tournament_settings SET lock_date = '2026-03-20T16:00:00Z', is_locked = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE tournament_settings SET lock_date = '2026-03-20T04:59:00Z', is_locked = false;");
        }
    }
}
