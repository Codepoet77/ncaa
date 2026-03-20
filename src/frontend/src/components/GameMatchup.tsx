import type { Game, Team } from '../types/index.ts';

interface GameMatchupProps {
  game: Game;
  pickedTeamId: number | null;
  isLocked: boolean;
  eliminatedTeamIds: Set<number>;
  onPickTeam: (gameId: number, teamId: number) => void;
}

function isTBD(team: Team | null): boolean {
  return !team || team.name === 'TBD';
}

function getTeamClassName(
  team: Team | null,
  game: Game,
  pickedTeamId: number | null,
  eliminatedTeamIds: Set<number>
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
  } else if (eliminatedTeamIds.has(team!.id)) {
    classes.push('team-eliminated');
  }

  return classes.join(' ');
}

function renderTeam(team: Team | null, game: Game, eliminatedTeamIds: Set<number>) {
  if (isTBD(team)) {
    return <span className="team-tbd">TBD</span>;
  }
  const isEliminated = eliminatedTeamIds.has(team!.id);
  return (
    <>
      <span className="team-seed">{team!.seed}</span>
      {team!.logoUrl && (
        <img
          src={team!.logoUrl}
          alt=""
          className={`team-logo${isEliminated ? ' team-logo-eliminated' : ''}`}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
      )}
      <span className={`team-name${isEliminated ? ' team-name-eliminated' : ''}`}>
        {team!.shortName || team!.name}
      </span>
      {game.isCompleted && game.team1Score != null && team!.id === game.team1?.id && (
        <span className="team-score">{game.team1Score}</span>
      )}
      {game.isCompleted && game.team2Score != null && team!.id === game.team2?.id && (
        <span className="team-score">{game.team2Score}</span>
      )}
    </>
  );
}

export default function GameMatchup({ game, pickedTeamId, isLocked, eliminatedTeamIds, onPickTeam }: GameMatchupProps) {
  const handlePickTeam = (team: Team | null) => {
    if (isTBD(team) || isLocked || game.isCompleted) return;
    if (eliminatedTeamIds.has(team!.id)) return;
    onPickTeam(game.id, team!.id);
  };

  const canClick = (team: Team | null) =>
    !isTBD(team) && !isLocked && !game.isCompleted && !eliminatedTeamIds.has(team!.id);

  return (
    <div className="game-matchup">
      <div
        className={getTeamClassName(game.team1, game, pickedTeamId, eliminatedTeamIds)}
        onClick={() => handlePickTeam(game.team1)}
        role={canClick(game.team1) ? 'button' : undefined}
        tabIndex={canClick(game.team1) ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team1);
        }}
      >
        {renderTeam(game.team1, game, eliminatedTeamIds)}
      </div>

      <div
        className={getTeamClassName(game.team2, game, pickedTeamId, eliminatedTeamIds)}
        onClick={() => handlePickTeam(game.team2)}
        role={canClick(game.team2) ? 'button' : undefined}
        tabIndex={canClick(game.team2) ? 0 : undefined}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') handlePickTeam(game.team2);
        }}
      >
        {renderTeam(game.team2, game, eliminatedTeamIds)}
      </div>
    </div>
  );
}
