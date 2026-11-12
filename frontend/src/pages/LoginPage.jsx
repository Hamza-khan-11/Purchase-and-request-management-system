import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useAuth } from '../context/useAuth';
import { getApiError } from '../services/api';
import { mapServerErrors } from '../utils/forms';

const DEMO_ACCOUNTS = [
  { label: 'Requester', email: 'requester1@demo.com', password: 'Requester@123' },
  { label: 'Manager', email: 'manager@demo.com', password: 'Manager@123' },
  { label: 'Officer', email: 'officer@demo.com', password: 'Officer@123' },
  { label: 'Admin', email: 'admin@demo.com', password: 'Admin@123' },
];

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [submitting, setSubmitting] = useState(false);

  const from = location.state?.from?.pathname || '/dashboard';

  if (user) {
    return <Navigate to={from} replace />;
  }

  const validate = () => {
    const found = {};
    if (!form.email.trim()) found.email = 'Email is required.';
    else if (!/^\S+@\S+\.\S+$/.test(form.email)) found.email = 'Enter a valid email address.';
    if (!form.password) found.password = 'Password is required.';
    return found;
  };

  const handleChange = (event) => {
    setForm({ ...form, [event.target.name]: event.target.value });
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setSubmitting(true);
    try {
      const loggedIn = await login(form.email.trim(), form.password);
      toast.success(`Welcome, ${loggedIn.fullName}`);
      navigate(from, { replace: true });
    } catch (error) {
      const apiError = getApiError(error);
      setErrors(mapServerErrors(apiError.errors));
      toast.error(apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="container min-vh-100 d-flex align-items-center justify-content-center py-4">
      <div className="card shadow-sm w-100" style={{ maxWidth: 420 }}>
        <div className="card-body p-4">
          <h1 className="h4 mb-1">Purchase Approval System</h1>
          <p className="text-muted mb-4">Sign in to continue</p>

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="email" className="form-label">Email</label>
              <input
                id="email"
                name="email"
                type="email"
                className={`form-control${errors.email ? ' is-invalid' : ''}`}
                value={form.email}
                onChange={handleChange}
                autoComplete="username"
              />
              {errors.email && <div className="invalid-feedback">{errors.email}</div>}
            </div>

            <div className="mb-3">
              <label htmlFor="password" className="form-label">Password</label>
              <input
                id="password"
                name="password"
                type="password"
                className={`form-control${errors.password ? ' is-invalid' : ''}`}
                value={form.password}
                onChange={handleChange}
                autoComplete="current-password"
              />
              {errors.password && <div className="invalid-feedback">{errors.password}</div>}
            </div>

            <button type="submit" className="btn btn-primary w-100" disabled={submitting}>
              {submitting ? 'Signing in...' : 'Sign in'}
            </button>
          </form>

          {import.meta.env.DEV && (
            <div className="mt-4">
              <div className="small text-muted mb-2">Demo accounts for ease hardcoded hain</div>
              <div className="d-flex flex-wrap gap-2">
                {DEMO_ACCOUNTS.map((account) => (
                  <button
                    key={account.email}
                    type="button"
                    className="btn btn-outline-secondary btn-sm"
                    onClick={() => setForm({ email: account.email, password: account.password })}
                  >
                    {account.label}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
