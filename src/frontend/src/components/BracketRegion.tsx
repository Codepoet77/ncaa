import type { Game } from '../types/index.ts';
import GameMatchup from './GameMatchup.tsx';

interface BracketRegionProps {
  regionName: string;
  games: Game[];
  picks: Map<number, number>;
  isLocked: boolean;
  onPickTeam: (gameId: number, teamId: number) => void;
  side: 'left' | 'right';
}

const ROUND_LABELS: Record<number, string> = {
  1: 'Round of 64 - 1 pt',
  2: 'Round of 32 - 2 pts',
  3: 'Sweet 16 - 3 pts',
  4: 'Elite 8 - 4 pts',
};

export default function BracketRegion({
  regionName,
  games,
  picks,
  isLocked,
  onPickTeam,
  side,
}: BracketRegionProps) {
  const rounds = [1, 2, 3, 4];
  const gamesByRound = new Map<number, Game[]>();

  for (const round of rounds) {
    const roundGames = games
      .filter((g) => g.round === round)
      .sort((a, b) => a.bracketPosition - b.bracketPosition);
    gamesByRound.set(round, roundGames);
  }

  const orderedRounds = side === 'left' ? rounds : [...rounds].reverse();

  return (
    <div className={`bracket-region bracket-region-${side}`}>
      <h3 className="region-title">{regionName}</h3>
      <div className="region-rounds">
        {orderedRounds.map((round) => (
          <div key={round} className={`round-column round-${round}`}>
            <div className="round-label">{ROUND_LABELS[round]}</div>
            <div className="round-games">
              {(gamesByRound.get(round) || []).map((game) => (
                <GameMatchup
                  key={game.id}
                  game={game}
                  pickedTeamId={picks.get(game.id) ?? null}
                  isLocked={isLocked}
                  onPickTeam={onPickTeam}
                />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
