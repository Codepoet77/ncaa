import type { Game, Team } from '../types/index.ts';

interface GameMatchupProps {
  game: Game;
  pickedTeamId: number | null;
  isLocked: boolean;
  onPickTeam: (gameId: number, teamId: number) => void;
}

function getTeamClassName(
  team: Team | null,
  game: Game,
  pickedTeamId: number | null
): string {
  if (!team) return 'team-slot team-empty';

  const classes = ['team-slot'];

  if (pickedTeamId === team.id) {
    classes.push('team-picked');
  }

  if (game.isCompleted && game.winnerId != null) {
    if (pickedTeamId === team.id) {
      if (game.winnerId === team.id) {
        classes.push('team-correct');
      } else {
        classes.push('team-incorrect');
      }
    }
    if (game.winnerId === team.id) {
      classes.push('team-winner');
    }
  }

  return classes.join(' ');
}

export default function GameMatchup({ game, pickedTeamId, isLocked, onPickTeam }: GameMatchupProps) {
  const handlePickTeam = (team: Team | null) => {
    if (!team || isLocked || game.isCompleted) return;
    onPickTeam(game.id, team.id);
  };

  return (
    <div className="game-matchup">
      <div
        className={getTeamClassName(game.team1, game, pickedTeamId)}
        onClick={() => handlePickTeam(game.team1)}
        role={game.team1 && !isLocked && !game.isCompleted ? 'button' : undefined}
        tabIndex={game.team1 && !isLocked && !game.isCompleted ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team1);
        }}
      >
        {game.team1 ? (
          <>
            <span className="team-seed">{game.team1.seed}</span>
            {game.team1.logoUrl && (
              <img
                src={game.team1.logoUrl}
                alt=""
                className="team-logo"
                onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
              />
            )}
            <span className="team-name">{game.team1.shortName || game.team1.name}</span>
            {game.isCompleted && game.team1Score != null && (
              <span className="team-score">{game.team1Score}</span>
            )}
          </>
        ) : (
          <span className="team-tbd">TBD</span>
        )}
      </div>

      <div
        className={getTeamClassName(game.team2, game, pickedTeamId)}
        onClick={() => handlePickTeam(game.team2)}
        role={game.team2 && !isLocked && !game.isCompleted ? 'button' : undefined}
        tabIndex={game.team2 && !isLocked && !game.isCompleted ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team2);
        }}
      >
        {game.team2 ? (
          <>
            <span className="team-seed">{game.team2.seed}</span>
            {game.team2.logoUrl && (
              <img
                src={game.team2.logoUrl}
                alt=""
                className="team-logo"
                onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
              />
            )}
            <span className="team-name">{game.team2.shortName || game.team2.name}</span>
            {game.isCompleted && game.team2Score != null && (
              <span className="team-score">{game.team2Score}</span>
            )}
          </>
        ) : (
          <span className="team-tbd">TBD</span>
        )}
      </div>
    </div>
  );
}
