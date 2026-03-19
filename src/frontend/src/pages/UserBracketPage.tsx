import { useState, useEffect, useMemo } from 'react';
import { useParams } from 'react-router';
import type { Game } from '../types/index.ts';
import { getBracket, getUserPicks } from '../services/api.ts';
import { buildProjectedGames } from '../utils/bracketLogic.ts';
import BracketRegion from '../components/BracketRegion.tsx';
import FinalFour from '../components/FinalFour.tsx';

const REGIONS_LEFT = ['East', 'South'];
const REGIONS_RIGHT = ['West', 'Midwest'];

export default function UserBracketPage() {
  const { userId } = useParams<{ userId: string }>();
  const [games, setGames] = useState<Game[]>([]);
  const [picks, setPicks] = useState<Map<number, number>>(new Map());
  const [bracketTitle, setBracketTitle] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      if (!userId) return;
      try {
        setLoading(true);
        const [bracketData, userData] = await Promise.all([
          getBracket(),
          getUserPicks(userId),
        ]);
        setGames(bracketData);
        setBracketTitle(userData.user.bracketTitle || `${userData.user.displayName}'s Bracket`);
        setDisplayName(userData.user.displayName);

        const picksMap = new Map<number, number>();
        for (const pick of userData.picks) {
          picksMap.set(pick.gameId, pick.pickedTeamId);
        }
        setPicks(picksMap);
      } catch (err) {
        setError('Failed to load bracket. Please try again later.');
        console.error('Failed to load user bracket:', err);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [userId]);

  const projectedGames = useMemo(
    () => buildProjectedGames(games, picks),
    [games, picks]
  );

  if (loading) {
    return (
      <div className="bracket-loading">
        <div className="loading-spinner" />
        <p>Loading bracket...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bracket-error">
        <p>{error}</p>
      </div>
    );
  }

  const noop = () => {};

  const regionGames = (regionName: string) =>
    projectedGames.filter((g) => g.region === regionName && g.round <= 4);

  const finalFourGames = projectedGames.filter((g) => g.round >= 5);

  const completedGames = games.filter((g) => g.isCompleted).length;
  const correctPicks = Array.from(picks.entries()).filter(([gameId]) => {
    const game = games.find((g) => g.id === gameId);
    return game?.isCompleted && game.winnerId === picks.get(gameId);
  }).length;

  return (
    <div className="page bracket-page">
      <div className="bracket-container">
        <div className="bracket-title-bar">
          <h2 className="bracket-title">{bracketTitle}</h2>
          <p className="bracket-subtitle">by {displayName}</p>
          {completedGames > 0 && (
            <p className="bracket-stats">{correctPicks} correct out of {completedGames} decided</p>
          )}
        </div>

        <div className="bracket-layout">
          <div className="bracket-left">
            {REGIONS_LEFT.map((region) => (
              <BracketRegion
                key={region}
                regionName={region}
                games={regionGames(region)}
                picks={picks}
                isLocked={true}
                onPickTeam={noop}
                side="left"
              />
            ))}
          </div>

          <div className="bracket-center">
            <FinalFour
              games={finalFourGames}
              picks={picks}
              isLocked={true}
              onPickTeam={noop}
            />
          </div>

          <div className="bracket-right">
            {REGIONS_RIGHT.map((region) => (
              <BracketRegion
                key={region}
                regionName={region}
                games={regionGames(region)}
                picks={picks}
                isLocked={true}
                onPickTeam={noop}
                side="right"
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
