-- NCAA March Madness Bracket Database Schema

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Users (from Google OAuth)
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    google_id VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    avatar_url TEXT,
    bracket_title VARCHAR(255),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Teams in the tournament
CREATE TABLE teams (
    id SERIAL PRIMARY KEY,
    espn_id VARCHAR(50) UNIQUE,
    name VARCHAR(255) NOT NULL,
    seed INT NOT NULL,
    region VARCHAR(50) NOT NULL,
    logo_url TEXT,
    short_name VARCHAR(50)
);

-- Tournament games (bracket structure)
CREATE TABLE games (
    id SERIAL PRIMARY KEY,
    espn_id VARCHAR(50) UNIQUE,
    round INT NOT NULL CHECK (round BETWEEN 1 AND 6),
    region VARCHAR(50),
    bracket_position INT NOT NULL,
    team1_id INT REFERENCES teams(id),
    team2_id INT REFERENCES teams(id),
    winner_id INT REFERENCES teams(id),
    team1_score INT,
    team2_score INT,
    game_time TIMESTAMPTZ,
    is_completed BOOLEAN DEFAULT FALSE,
    next_game_id INT REFERENCES games(id),
    slot INT
);

-- User bracket picks
CREATE TABLE user_picks (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    game_id INT NOT NULL REFERENCES games(id) ON DELETE CASCADE,
    picked_team_id INT NOT NULL REFERENCES teams(id),
    is_correct BOOLEAN,
    points_earned INT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, game_id)
);

-- Tournament settings
CREATE TABLE tournament_settings (
    id SERIAL PRIMARY KEY,
    year INT NOT NULL DEFAULT 2026,
    lock_date TIMESTAMPTZ NOT NULL,
    is_locked BOOLEAN DEFAULT FALSE,
    last_espn_sync TIMESTAMPTZ
);

-- Insert default tournament settings (lock before first game, March 19 2026)
INSERT INTO tournament_settings (year, lock_date, is_locked)
VALUES (2026, '2026-03-19T12:00:00Z', FALSE);

-- Indexes
CREATE INDEX idx_user_picks_user_id ON user_picks(user_id);
CREATE INDEX idx_user_picks_game_id ON user_picks(game_id);
CREATE INDEX idx_games_round ON games(round);
CREATE INDEX idx_games_region ON games(region);
CREATE INDEX idx_teams_region ON teams(region);
