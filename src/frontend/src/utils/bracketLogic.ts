import type { Game, Team } from '../types/index.ts';

/**
 * Build projected games by overlaying user picks onto future rounds.
 * When a user picks a team to win a game, that team appears in the
 * corresponding next-round matchup slot.
 *
 * Bracket position mapping:
 *   R1 pos 1,2 → R2 pos 1
 *   R1 pos 3,4 → R2 pos 2
 *   etc.
 * Within the next game: odd position → team1 slot, even → team2 slot.
 */
export function buildProjectedGames(
  games: Game[],
  picks: Map<number, number>
): Game[] {
  // Index games by region + round + bracketPosition
  const gameIndex = new Map<string, Game>();
  for (const g of games) {
    gameIndex.set(`${g.region}|${g.round}|${g.bracketPosition}`, { ...g });
  }

  // Build a team lookup from all games
  const teamMap = new Map<number, Team>();
  for (const g of games) {
    if (g.team1) teamMap.set(g.team1.id, g.team1);
    if (g.team2) teamMap.set(g.team2.id, g.team2);
  }

  // Process rounds in order so results and picks cascade
  const maxRound = Math.max(...games.map((g) => g.round));
  const regions = [...new Set(games.map((g) => g.region))];

  for (let round = 1; round <= maxRound; round++) {
    for (const region of regions) {
      const roundGames = games
        .filter((g) => g.region === region && g.round === round)
        .sort((a, b) => a.bracketPosition - b.bracketPosition);

      for (const game of roundGames) {
        // Determine the advancing team: actual winner first, then user pick
        let advancingTeam: Team | null = null;

        const projectedGame = gameIndex.get(`${game.region}|${game.round}|${game.bracketPosition}`);

        if (game.isCompleted && game.winnerId != null) {
          // Actual result — use the winner
          advancingTeam = teamMap.get(game.winnerId) ?? null;
        } else {
          // User pick
          const pickedTeamId = picks.get(game.id);
          if (pickedTeamId != null) {
            advancingTeam =
              teamMap.get(pickedTeamId) ??
              (projectedGame?.team1?.id === pickedTeamId ? projectedGame.team1 : null) ??
              (projectedGame?.team2?.id === pickedTeamId ? projectedGame.team2 : null);
          }
        }

        if (!advancingTeam) continue;

        // Determine which next-round game this feeds into
        const nextRound = round + 1;
        const nextPosition = Math.ceil(game.bracketPosition / 2);
        const isTeam1Slot = game.bracketPosition % 2 === 1;

        // Handle cross-region (Final Four / Championship)
        let nextRegion = region;
        if (round === 4) nextRegion = 'Final Four';
        if (region === 'Final Four' && round === 5) nextRegion = 'TBD';

        const nextKey = `${nextRegion}|${nextRound}|${nextPosition}`;
        const nextGame = gameIndex.get(nextKey);

        if (nextGame && !nextGame.isCompleted) {
          if (isTeam1Slot) {
            if (!nextGame.team1 || nextGame.team1.name === 'TBD') {
              nextGame.team1 = { ...advancingTeam };
            }
          } else {
            if (!nextGame.team2 || nextGame.team2.name === 'TBD') {
              nextGame.team2 = { ...advancingTeam };
            }
          }
        }
      }
    }
  }

  return Array.from(gameIndex.values());
}

/**
 * When a pick is changed or removed, clear any cascading picks
 * that depended on it in later rounds.
 */
export function clearCascadingPicks(
  games: Game[],
  picks: Map<number, number>,
  changedGameId: number,
  previousTeamId: number | undefined
): Map<number, number> {
  if (previousTeamId == null) return picks;

  const game = games.find((g) => g.id === changedGameId);
  if (!game) return picks;

  const newPicks = new Map(picks);
  clearDownstream(games, newPicks, game, previousTeamId);
  return newPicks;
}

function clearDownstream(
  games: Game[],
  picks: Map<number, number>,
  game: Game,
  removedTeamId: number
) {
  const nextRound = game.round + 1;
  const nextPosition = Math.ceil(game.bracketPosition / 2);
  let nextRegion = game.region;
  if (game.round === 4) nextRegion = 'Final Four';
  if (game.region === 'Final Four' && game.round === 5) nextRegion = 'TBD';

  const nextGame = games.find(
    (g) => g.region === nextRegion && g.round === nextRound && g.bracketPosition === nextPosition
  );

  if (!nextGame) return;

  // If the next game had the removed team as its pick, clear it and cascade further
  const nextPick = picks.get(nextGame.id);
  if (nextPick === removedTeamId) {
    picks.delete(nextGame.id);
    clearDownstream(games, picks, nextGame, removedTeamId);
  }
}
