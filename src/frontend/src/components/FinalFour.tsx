import type { Game } from '../types/index.ts';
import GameMatchup from './GameMatchup.tsx';

interface FinalFourProps {
  games: Game[];
  picks: Map<number, number>;
  isLocked: boolean;
  onPickTeam: (gameId: number, teamId: number) => void;
}

export default function FinalFour({ games, picks, isLocked, onPickTeam }: FinalFourProps) {
  const semiFinals = games
    .filter((g) => g.round === 5)
    .sort((a, b) => a.bracketPosition - b.bracketPosition);
  const championship = games.filter((g) => g.round === 6);

  return (
    <div className="final-four">
      <h3 className="region-title final-four-title">Final Four</h3>
      <div className="final-four-layout">
        <div className="semi-finals">
          <div className="round-label">Final Four - 5 pts</div>
          <div className="semi-final-games">
            {semiFinals.map((game) => (
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

        <div className="championship">
          <div className="round-label">Championship - 6 pts</div>
          <div className="championship-games">
            {championship.map((game) => (
              <GameMatchup
                key={game.id}
                game={game}
                pickedTeamId={picks.get(game.id) ?? null}
                isLocked={isLocked}
                onPickTeam={onPickTeam}
              />
            ))}
          </div>
          {championship.length > 0 && picks.get(championship[0].id) && (
            <div className="champion-display">
              <div className="champion-trophy">🏆</div>
              <div className="champion-label">Champion</div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
