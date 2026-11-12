export default function Pagination({ page, totalPages, totalCount, pageSize, onChange }) {
  if (!totalCount) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  // Show at most 5 page numbers around the current page
  const start = Math.max(1, Math.min(page - 2, totalPages - 4));
  const end = Math.min(totalPages, start + 4);
  const pages = [];
  for (let p = start; p <= end; p += 1) pages.push(p);

  return (
    <div className="d-flex flex-column flex-md-row justify-content-between align-items-center gap-2 mt-3">
      <div className="small text-muted">
        Showing {from}-{to} of {totalCount}
      </div>
      <nav aria-label="Pagination">
        <ul className="pagination pagination-sm mb-0">
          <li className={`page-item${page <= 1 ? ' disabled' : ''}`}>
            <button type="button" className="page-link" disabled={page <= 1} onClick={() => onChange(page - 1)}>
              Previous
            </button>
          </li>
          {pages.map((p) => (
            <li key={p} className={`page-item${p === page ? ' active' : ''}`}>
              <button type="button" className="page-link" onClick={() => onChange(p)}>
                {p}
              </button>
            </li>
          ))}
          <li className={`page-item${page >= totalPages ? ' disabled' : ''}`}>
            <button type="button" className="page-link" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
              Next
            </button>
          </li>
        </ul>
      </nav>
    </div>
  );
}
