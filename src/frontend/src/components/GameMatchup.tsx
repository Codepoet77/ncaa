import type { Game, Team } from '../types/index.ts';

interface GameMatchupProps {
  game: Game;
  pickedTeamId: number | null;
  isLocked: boolean;
  onPickTeam: (gameId: number, teamId: number) => void;
}

function isTBD(team: Team | null): boolean {
  return !team || team.name === 'TBD';
}

function getTeamClassName(
  team: Team | null,
  game: Game,
  pickedTeamId: number | null
): string {
  if (isTBD(team)) return 'team-slot team-empty';

  const classes = ['team-slot'];

  if (pickedTeamId === team!.id) {
    classes.push('team-picked');
  }

  if (game.isCompleted && game.winnerId != null) {
    if (pickedTeamId === team!.id) {
      if (game.winnerId === team!.id) {
        classes.push('team-correct');
      } else {
        classes.push('team-incorrect');
      }
    }
    if (game.winnerId === team!.id) {
      classes.push('team-winner');
    }
  }

  return classes.join(' ');
}

function renderTeam(team: Team | null, game: Game) {
  if (isTBD(team)) {
    return <span className="team-tbd">TBD</span>;
  }
  return (
    <>
      <span className="team-seed">{team!.seed}</span>
      {team!.logoUrl && (
        <img
          src={team!.logoUrl}
          alt=""
          className="team-logo"
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
      )}
      <span className="team-name">{team!.shortName || team!.name}</span>
      {game.isCompleted && game.team1Score != null && team!.id === game.team1?.id && (
        <span className="team-score">{game.team1Score}</span>
      )}
      {game.isCompleted && game.team2Score != null && team!.id === game.team2?.id && (
        <span className="team-score">{game.team2Score}</span>
      )}
    </>
  );
}

export default function GameMatchup({ game, pickedTeamId, isLocked, onPickTeam }: GameMatchupProps) {
  const handlePickTeam = (team: Team | null) => {
    if (isTBD(team) || isLocked || game.isCompleted) return;
    onPickTeam(game.id, team!.id);
  };

  const canClick = (team: Team | null) => !isTBD(team) && !isLocked && !game.isCompleted;

  return (
    <div className="game-matchup">
      <div
        className={getTeamClassName(game.team1, game, pickedTeamId)}
        onClick={() => handlePickTeam(game.team1)}
        role={canClick(game.team1) ? 'button' : undefined}
        tabIndex={canClick(game.team1) ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team1);
        }}
      >
        {renderTeam(game.team1, game)}
      </div>

      <div
        className={getTeamClassName(game.team2, game, pickedTeamId)}
        onClick={() => handlePickTeam(game.team2)}
        role={canClick(game.team2) ? 'button' : undefined}
        tabIndex={canClick(game.team2) ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team2);
        }}
      >
        {renderTeam(game.team2, game)}
      </div>
    </div>
  );
}
