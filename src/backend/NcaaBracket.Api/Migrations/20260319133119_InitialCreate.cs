using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NcaaBracket.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    espn_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    seed = table.Column<int>(type: "integer", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    short_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tournament_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    year = table.Column<int>(type: "integer", nullable: false),
                    lock_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    last_espn_sync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    google_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    bracket_title = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    espn_id = table.Column<string>(type: "text", nullable: true),
                    round = table.Column<int>(type: "integer", nullable: false),
                    region = table.Column<string>(type: "text", nullable: true),
                    bracket_position = table.Column<int>(type: "integer", nullable: false),
                    team1_id = table.Column<int>(type: "integer", nullable: true),
                    team2_id = table.Column<int>(type: "integer", nullable: true),
                    winner_id = table.Column<int>(type: "integer", nullable: true),
                    team1_score = table.Column<int>(type: "integer", nullable: true),
                    team2_score = table.Column<int>(type: "integer", nullable: true),
                    game_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    next_game_id = table.Column<int>(type: "integer", nullable: true),
                    slot = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.id);
                    table.ForeignKey(
                        name: "FK_games_games_next_game_id",
                        column: x => x.next_game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_games_teams_team1_id",
                        column: x => x.team1_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_games_teams_team2_id",
                        column: x => x.team2_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_games_teams_winner_id",
                        column: x => x.winner_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_picks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<int>(type: "integer", nullable: false),
                    picked_team_id = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: true),
                    points_earned = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_picks", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_picks_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_picks_teams_picked_team_id",
                        column: x => x.picked_team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_picks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_games_espn_id",
                table: "games",
                column: "espn_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_games_next_game_id",
                table: "games",
                column: "next_game_id");

            migrationBuilder.CreateIndex(
                name: "IX_games_team1_id",
                table: "games",
                column: "team1_id");

            migrationBuilder.CreateIndex(
                name: "IX_games_team2_id",
                table: "games",
                column: "team2_id");

            migrationBuilder.CreateIndex(
                name: "IX_games_winner_id",
                table: "games",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_espn_id",
                table: "teams",
                column: "espn_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_picks_game_id",
                table: "user_picks",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_picks_picked_team_id",
                table: "user_picks",
                column: "picked_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_picks_user_id_game_id",
                table: "user_picks",
                columns: new[] { "user_id", "game_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_google_id",
                table: "users",
                column: "google_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_settings");

            migrationBuilder.DropTable(
                name: "user_picks");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "teams");
        }
    }
}
