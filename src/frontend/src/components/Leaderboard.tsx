import { useState, useEffect } from 'react';
import { Link } from 'react-router';
import type { LeaderboardEntry } from '../types/index.ts';
import { getLeaderboard } from '../services/api.ts';
import { useAuth } from '../context/AuthContext.tsx';

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return d.toLocaleString('en-US', {
    month: 'short', day: 'numeric',
    hour: 'numeric', minute: '2-digit',
    hour12: true,
  });
}

export default function Leaderboard() {
  const { user } = useAuth();
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isAdmin = user?.role === 'admin';

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
                <th className="col-max-points">Max Possible</th>
                {isAdmin && <th className="col-last-pick">Last Pick</th>}
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => {
                const isCurrentUser = user?.id === entry.userId;
                const bracketLink = isCurrentUser ? '/bracket' : `/bracket/${entry.userId}`;

                return (
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
                        <Link to={bracketLink} className="player-name-link">
                          {entry.displayName}
                        </Link>
                      </div>
                    </td>
                    <td className="col-bracket">
                      <Link to={bracketLink} className="bracket-link">
                        {entry.bracketTitle}
                      </Link>
                    </td>
                    <td className="col-correct">{entry.correctPicks}</td>
                    <td className="col-points">{entry.totalPoints}</td>
                    <td className="col-max-points">{entry.maxPossiblePoints}</td>
                    {isAdmin && (
                      <td className="col-last-pick">
                        {entry.lastPickAt ? formatDate(entry.lastPickAt) : '—'}
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
