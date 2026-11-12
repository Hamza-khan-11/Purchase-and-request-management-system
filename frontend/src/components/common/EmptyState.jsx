export default function EmptyState({ title, message, action }) {
  return (
    <div className="card">
      <div className="card-body text-center py-5">
        <h3 className="h5">{title}</h3>
        {message && <p className="text-muted mb-3">{message}</p>}
        {action}
      </div>
    </div>
  );
}
