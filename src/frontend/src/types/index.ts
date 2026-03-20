export interface Team {
  id: number;
  espnId?: string;
  name: string;
  seed: number;
  region: string;
  logoUrl?: string;
  shortName?: string;
}

export interface Game {
  id: number;
  round: number;
  region: string | null;
  bracketPosition: number;
  team1: Team | null;
  team2: Team | null;
  winnerId: number | null;
  team1Score: number | null;
  team2Score: number | null;
  gameTime: string | null;
  isCompleted: boolean;
  nextGameId: number | null;
  slot: number | null;
}

export interface UserPick {
  gameId: number;
  pickedTeamId: number;
}

export interface UserPickDto {
  id: number;
  gameId: number;
  pickedTeamId: number;
  pickedTeamName?: string;
  isCorrect?: boolean;
  pointsEarned: number;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl: string;
  bracketTitle: string;
  totalPoints: number;
  maxPossiblePoints: number;
  correctPicks: number;
  lastPickAt?: string;
  lastLoginAt?: string;
}

export interface TournamentSettings {
  year: number;
  lockDate: string;
  isLocked: boolean;
}

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string;
  bracketTitle?: string;
  role?: string;
}
