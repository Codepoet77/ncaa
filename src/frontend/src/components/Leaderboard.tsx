import { useState, useEffect } from 'react';
import type { LeaderboardEntry } from '../types/index.ts';
import { getLeaderboard } from '../services/api.ts';

export default function Leaderboard() {
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadLeaderboard() {
      try {
        setLoading(true);
        const data = await getLeaderboard();
        setEntries(data);
      } catch (err) {
        setError('Failed to load leaderboard. Please try again later.');
        console.error('Failed to load leaderboard:', err);
      } finally {
        setLoading(false);
      }
    }
    loadLeaderboard();
  }, []);

  if (loading) {
    return (
      <div className="bracket-loading">
        <div className="loading-spinner" />
        <p>Loading leaderboard...</p>
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

  return (
    <div className="leaderboard-container">
      <h2 className="leaderboard-title">Leaderboard</h2>
      {entries.length === 0 ? (
        <p className="leaderboard-empty">No entries yet. Be the first to submit your picks!</p>
      ) : (
        <div className="leaderboard-table-wrapper">
          <table className="leaderboard-table">
            <thead>
              <tr>
                <th className="col-rank">Rank</th>
                <th className="col-player">Player</th>
                <th className="col-bracket">Bracket</th>
                <th className="col-correct">Correct Picks</th>
                <th className="col-points">Points</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <tr
                  key={entry.userId}
                  className={`leaderboard-row ${entry.rank <= 3 ? `rank-${entry.rank}` : ''}`}
                >
                  <td className="col-rank">
                    <span className="rank-badge">
                      {entry.rank === 1 && '🥇'}
                      {entry.rank === 2 && '🥈'}
                      {entry.rank === 3 && '🥉'}
                      {entry.rank > 3 && entry.rank}
                    </span>
                  </td>
                  <td className="col-player">
                    <div className="player-info">
                      <div className="player-avatar-placeholder">
                        {entry.displayName?.charAt(0)?.toUpperCase() || '?'}
                      </div>
                      <span className="player-name">{entry.displayName}</span>
                    </div>
                  </td>
                  <td className="col-bracket">{entry.bracketTitle}</td>
                  <td className="col-correct">{entry.correctPicks}</td>
                  <td className="col-points">{entry.totalPoints}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
