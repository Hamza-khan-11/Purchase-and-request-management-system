import { Link } from 'react-router-dom';

export default function UnauthorizedPage() {
  return (
    <div className="text-center py-5">
      <div className="display-1 text-danger">403</div>
      <h2 className="h4">No permission</h2>
      <p className="text-muted">Your role does not allow you to open this page.</p>
      <Link to="/dashboard" className="btn btn-primary">Back to dashboard</Link>
    </div>
  );
}
