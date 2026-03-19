import axios from 'axios';
import type { Game, TournamentSettings, UserPick, UserPickDto, LeaderboardEntry, User } from '../types/index.ts';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export async function googleLogin(idToken: string): Promise<{ token: string; user: User }> {
  const response = await api.post('/api/auth/google', { idToken });
  return response.data;
}

export async function getBracket(): Promise<Game[]> {
  const response = await api.get('/api/bracket');
  return response.data;
}

export async function getSettings(): Promise<TournamentSettings> {
  const response = await api.get('/api/bracket/settings');
  return response.data;
}

export async function getPicks(): Promise<UserPickDto[]> {
  const response = await api.get('/api/picks');
  return response.data;
}

export async function submitPicks(picks: UserPick[]): Promise<void> {
  await api.post('/api/picks', { picks });
}

export async function getBracketTitle(): Promise<string> {
  const response = await api.get('/api/picks/title');
  return response.data.title;
}

export async function updateBracketTitle(title: string): Promise<void> {
  await api.put('/api/picks/title', { title });
}

export async function getLeaderboard(): Promise<LeaderboardEntry[]> {
  const response = await api.get('/api/leaderboard');
  return response.data;
}

export default api;
