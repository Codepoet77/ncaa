import { Navigate, Link } from 'react-router';
import { useAuth } from '../context/AuthContext.tsx';

export default function Home() {
  const { isAuthenticated } = useAuth();

  if (isAuthenticated) {
    return <Navigate to="/bracket" replace />;
  }

  return (
    <div className="home-page">
      <div className="home-hero">
        <div className="home-basketball">🏀</div>
        <h1 className="home-title">March Madness 2026</h1>
        <p className="home-subtitle">
          Fill out your bracket. Compete with friends. Win bragging rights.
        </p>
        <div className="home-actions">
          <Link to="/bracket" className="home-cta">
            View Bracket
          </Link>
          <Link to="/leaderboard" className="home-cta home-cta-secondary">
            Leaderboard
          </Link>
        </div>
        <p className="home-signin-note">
          Sign in with Google to start making your picks!
        </p>
      </div>

      <div className="home-scoring">
        <h2>Scoring</h2>
        <div className="scoring-grid">
          <div className="scoring-item">
            <span className="scoring-round">Round of 64</span>
            <span className="scoring-points">1 pt</span>
          </div>
          <div className="scoring-item">
            <span className="scoring-round">Round of 32</span>
            <span className="scoring-points">2 pts</span>
          </div>
          <div className="scoring-item">
            <span className="scoring-round">Sweet 16</span>
            <span className="scoring-points">4 pts</span>
          </div>
          <div className="scoring-item">
            <span className="scoring-round">Elite 8</span>
            <span className="scoring-points">8 pts</span>
          </div>
          <div className="scoring-item">
            <span className="scoring-round">Final Four</span>
            <span className="scoring-points">16 pts</span>
          </div>
          <div className="scoring-item">
            <span className="scoring-round">Championship</span>
            <span className="scoring-points">32 pts</span>
          </div>
        </div>
      </div>
    </div>
  );
}
