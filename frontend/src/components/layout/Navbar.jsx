import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/useAuth';
import { ROLE_LABELS } from '../../utils/roles';

export default function Navbar({ onToggleSidebar }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="navbar navbar-dark bg-dark px-3" style={{ minHeight: 56 }}>
      <div className="d-flex align-items-center gap-2">
        <button
          type="button"
          className="btn btn-outline-light d-md-none"
          onClick={onToggleSidebar}
          aria-label="Toggle menu"
        >
          &#9776;
        </button>
        <span className="navbar-brand mb-0 h1">Purchase Approval</span>
      </div>

      <div className="d-flex align-items-center gap-3">
        <div className="text-end d-none d-sm-block">
          <div className="fw-semibold small text-light">{user.fullName}</div>
          <div className="small text-secondary">{ROLE_LABELS[user.role] || user.role}</div>
        </div>
        <button type="button" className="btn btn-outline-light btn-sm" onClick={handleLogout}>
          Logout
        </button>
      </div>
    </header>
  );
}
