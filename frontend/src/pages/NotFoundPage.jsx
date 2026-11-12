import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="container text-center py-5">
      <div className="display-1 text-secondary">404</div>
      <h2 className="h4">Page not found</h2>
      <p className="text-muted">The page you are looking for does not exist.</p>
      <Link to="/dashboard" className="btn btn-primary">Go to dashboard</Link>
    </div>
  );
}
