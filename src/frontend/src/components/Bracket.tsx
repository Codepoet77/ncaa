import { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import type { Game, UserPick } from '../types/index.ts';
import type { TournamentSettings } from '../types/index.ts';
import { getBracket, getSettings, getPicks, submitPicks, getBracketTitle, updateBracketTitle } from '../services/api.ts';
import { useAuth } from '../context/AuthContext.tsx';
import { buildProjectedGames, clearCascadingPicks } from '../utils/bracketLogic.ts';
import BracketRegion from './BracketRegion.tsx';
import FinalFour from './FinalFour.tsx';

const REGIONS_LEFT = ['East', 'South'];
const REGIONS_RIGHT = ['West', 'Midwest'];

export default function Bracket() {
  const { isAuthenticated, updateUser } = useAuth();
  const [games, setGames] = useState<Game[]>([]);
  const [settings, setSettings] = useState<TournamentSettings | null>(null);
  const [picks, setPicks] = useState<Map<number, number>>(new Map());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [bracketTitle, setBracketTitle] = useState<string>('');
  const [editingTitle, setEditingTitle] = useState(false);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const saveTimeoutRef = useRef<ReturnType<typeof setTimeout>>(undefined);
  const initialLoadRef = useRef(true);

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);
        const [bracketData, settingsData] = await Promise.all([
          getBracket(),
          getSettings(),
        ]);
        setGames(bracketData);
        setSettings(settingsData);

        if (isAuthenticated) {
          try {
            const title = await getBracketTitle();
            setBracketTitle(title);
          } catch {
            // fallback
          }
          try {
            const userPicks = await getPicks();
            const picksMap = new Map<number, number>();
            for (const pick of userPicks) {
              picksMap.set(pick.gameId, pick.pickedTeamId);
            }
            setPicks(picksMap);
          } catch {
            // User may not have picks yet
          }
        }
      } catch (err) {
        setError('Failed to load bracket data. Please try again later.');
        console.error('Failed to load bracket:', err);
      } finally {
        setLoading(false);
        // Mark initial load complete after a tick so the save effect doesn't fire
        setTimeout(() => { initialLoadRef.current = false; }, 100);
      }
    }
    loadData();
  }, [isAuthenticated]);

  // Auto-save picks whenever they change (debounced)
  useEffect(() => {
    if (initialLoadRef.current || !isAuthenticated || picks.size === 0) return;

    setSaveStatus('saving');
    if (saveTimeoutRef.current) clearTimeout(saveTimeoutRef.current);

    saveTimeoutRef.current = setTimeout(async () => {
      try {
        const picksArray: UserPick[] = Array.from(picks.entries()).map(
          ([gameId, pickedTeamId]) => ({ gameId, pickedTeamId })
        );
        await submitPicks(picksArray);
        setSaveStatus('saved');
        setTimeout(() => setSaveStatus('idle'), 2000);
      } catch (err) {
        setSaveStatus('error');
        console.error('Failed to auto-save picks:', err);
      }
    }, 500);

    return () => {
      if (saveTimeoutRef.current) clearTimeout(saveTimeoutRef.current);
    };
  }, [picks, isAuthenticated]);

  const projectedGames = useMemo(
    () => buildProjectedGames(games, picks),
    [games, picks]
  );

  const handlePickTeam = useCallback((gameId: number, teamId: number) => {
    setPicks((prev) => {
      const next = new Map(prev);
      const currentPick = next.get(gameId);

      if (currentPick === teamId) {
        next.delete(gameId);
        return clearCascadingPicks(games, next, gameId, teamId);
      } else {
        if (currentPick != null) {
          const cleared = clearCascadingPicks(games, next, gameId, currentPick);
          cleared.set(gameId, teamId);
          return cleared;
        }
        next.set(gameId, teamId);
        return next;
      }
    });
  }, [games]);

  const handleSaveTitle = async () => {
    if (!bracketTitle.trim()) return;
    try {
      await updateBracketTitle(bracketTitle.trim());
      updateUser({ bracketTitle: bracketTitle.trim() });
      setEditingTitle(false);
    } catch (err) {
      console.error('Failed to update title:', err);
    }
  };

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

  const isLocked = settings?.isLocked ?? false;

  const regionGames = (regionName: string) =>
    projectedGames.filter((g) => g.region === regionName && g.round <= 4);

  const finalFourGames = projectedGames.filter((g) => g.round >= 5);

  return (
    <div className="bracket-container">
      {isAuthenticated && bracketTitle && (
        <div className="bracket-title-bar">
          {editingTitle ? (
            <div className="bracket-title-edit">
              <input
                type="text"
                value={bracketTitle}
                onChange={(e) => setBracketTitle(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleSaveTitle(); if (e.key === 'Escape') setEditingTitle(false); }}
                className="bracket-title-input"
                maxLength={100}
                autoFocus
              />
              <button className="bracket-title-save-btn" onClick={handleSaveTitle}>Save</button>
              <button className="bracket-title-cancel-btn" onClick={() => setEditingTitle(false)}>Cancel</button>
            </div>
          ) : (
            <h2 className="bracket-title" onClick={() => !isLocked && setEditingTitle(true)}>
              {bracketTitle}
              {!isLocked && <span className="bracket-title-edit-icon">&#9998;</span>}
            </h2>
          )}
          {saveStatus === 'saving' && <span className="auto-save-status saving">Saving...</span>}
          {saveStatus === 'saved' && <span className="auto-save-status saved">Saved</span>}
          {saveStatus === 'error' && <span className="auto-save-status error">Failed to save</span>}
        </div>
      )}

      {isLocked && (
        <div className="locked-banner">
          Picks are locked - the tournament has begun!
        </div>
      )}

      {!isAuthenticated && (
        <div className="login-prompt">
          Sign in with Google to make your picks!
        </div>
      )}

      <div className="bracket-layout">
        <div className="bracket-left">
          {REGIONS_LEFT.map((region) => (
            <BracketRegion
              key={region}
              regionName={region}
              games={regionGames(region)}
              picks={picks}
              isLocked={isLocked || !isAuthenticated}
              onPickTeam={handlePickTeam}
              side="left"
            />
          ))}
        </div>

        <div className="bracket-center">
          <FinalFour
            games={finalFourGames}
            picks={picks}
            isLocked={isLocked || !isAuthenticated}
            onPickTeam={handlePickTeam}
          />
        </div>

        <div className="bracket-right">
          {REGIONS_RIGHT.map((region) => (
            <BracketRegion
              key={region}
              regionName={region}
              games={regionGames(region)}
              picks={picks}
              isLocked={isLocked || !isAuthenticated}
              onPickTeam={handlePickTeam}
              side="right"
            />
          ))}
        </div>
      </div>
    </div>
  );
}
