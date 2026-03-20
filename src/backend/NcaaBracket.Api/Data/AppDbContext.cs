using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Models;

namespace NcaaBracket.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<UserPick> UserPicks => Set<UserPick>();
    public DbSet<TournamentSettings> TournamentSettings => Set<TournamentSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GoogleId).HasColumnName("google_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.BracketTitle).HasColumnName("bracket_title");
            entity.Property(e => e.Role).HasColumnName("role").HasDefaultValue("user");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.GoogleId).IsUnique();
        });

        // Teams
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.EspnId).HasColumnName("espn_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Seed).HasColumnName("seed");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.ShortName).HasColumnName("short_name");
            entity.HasIndex(e => e.EspnId).IsUnique();
        });

        // Games
        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.EspnId).HasColumnName("espn_id");
            entity.Property(e => e.Round).HasColumnName("round");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.BracketPosition).HasColumnName("bracket_position");
            entity.Property(e => e.Team1Id).HasColumnName("team1_id");
            entity.Property(e => e.Team2Id).HasColumnName("team2_id");
            entity.Property(e => e.WinnerId).HasColumnName("winner_id");
            entity.Property(e => e.Team1Score).HasColumnName("team1_score");
            entity.Property(e => e.Team2Score).HasColumnName("team2_score");
            entity.Property(e => e.GameTime).HasColumnName("game_time");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            entity.Property(e => e.NextGameId).HasColumnName("next_game_id");
            entity.Property(e => e.Slot).HasColumnName("slot");
            entity.HasIndex(e => e.EspnId).IsUnique();

            entity.HasOne(e => e.Team1)
                .WithMany()
                .HasForeignKey(e => e.Team1Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Team2)
                .WithMany()
                .HasForeignKey(e => e.Team2Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Winner)
                .WithMany()
                .HasForeignKey(e => e.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.NextGame)
                .WithMany()
                .HasForeignKey(e => e.NextGameId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserPicks
        modelBuilder.Entity<UserPick>(entity =>
        {
            entity.ToTable("user_picks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.GameId).HasColumnName("game_id");
            entity.Property(e => e.PickedTeamId).HasColumnName("picked_team_id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.PointsEarned).HasColumnName("points_earned");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserPicks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Game)
                .WithMany()
                .HasForeignKey(e => e.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PickedTeam)
                .WithMany()
                .HasForeignKey(e => e.PickedTeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TournamentSettings
        modelBuilder.Entity<TournamentSettings>(entity =>
        {
            entity.ToTable("tournament_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.LockDate).HasColumnName("lock_date");
            entity.Property(e => e.IsLocked).HasColumnName("is_locked");
            entity.Property(e => e.LastEspnSync).HasColumnName("last_espn_sync");
        });
    }
}
