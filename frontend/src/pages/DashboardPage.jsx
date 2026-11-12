import { useEffect, useState } from 'react';
import { useAuth } from '../context/useAuth';
import api, { getApiError } from '../services/api';
import { ROLE_LABELS } from '../utils/roles';

export default function DashboardPage() {
  const { user } = useAuth();
  const [check, setCheck] = useState({ state: 'loading', text: '' });

  // Proves the token is attached and accepted by the API
  useEffect(() => {
    let cancelled = false;
    api
      .get('/api/auth/me')
      .then((response) => {
        if (!cancelled) {
          const me = response.data.data;
          setCheck({ state: 'ok', text: `${me.name} (${me.role}) confirmed by the API` });
        }
      })
      .catch((error) => {
        if (!cancelled) setCheck({ state: 'error', text: getApiError(error).message });
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div>
      <h2 className="h4">Dashboard</h2>
      <p className="text-muted">
        Welcome, {user.fullName} ({ROLE_LABELS[user.role] || user.role}). Charts and summary cards.
      </p>
      <div className={`alert ${check.state === 'ok' ? 'alert-success' : check.state === 'error' ? 'alert-danger' : 'alert-secondary'}`}>
        API check: {check.state === 'loading' ? 'checking...' : check.text}
      </div>
    </div>
  );
}
