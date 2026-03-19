import { Link, useNavigate } from 'react-router';
import { GoogleLogin } from '@react-oauth/google';
import { useAuth } from '../context/AuthContext.tsx';

export default function Header() {
  const { user, login, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleGoogleSuccess = async (credentialResponse: { credential?: string }) => {
    if (credentialResponse.credential) {
      try {
        await login(credentialResponse.credential);
      } catch (err) {
        console.error('Login failed:', err);
      }
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <header className="app-header">
      <div className="header-inner">
        <Link to="/" className="header-brand">
          <h1 className="header-title">March Madness 2026</h1>
        </Link>

        <nav className="header-nav">
          {isAuthenticated && (
            <>
              <Link to="/bracket" className="nav-link">Bracket</Link>
              <Link to="/leaderboard" className="nav-link">Leaderboard</Link>
            </>
          )}
        </nav>

        <div className="header-auth">
          {isAuthenticated && user ? (
            <div className="user-menu">
              <div className="user-avatar-placeholder">
                {user.displayName?.charAt(0)?.toUpperCase() || '?'}
              </div>
              <span className="user-name">{user.displayName}</span>
              <button onClick={handleLogout} className="logout-btn">
                Logout
              </button>
            </div>
          ) : (
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={() => console.error('Google login error')}
              theme="filled_blue"
              size="medium"
              shape="pill"
            />
          )}
        </div>
      </div>
    </header>
  );
}
