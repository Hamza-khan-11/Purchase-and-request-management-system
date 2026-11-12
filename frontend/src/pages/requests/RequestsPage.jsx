import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import EmptyState from '../../components/common/EmptyState';
import Loader from '../../components/common/Loader';
import Pagination from '../../components/common/Pagination';
import SortHeader from '../../components/common/SortHeader';
import RequestStatusBadge from '../../components/requests/RequestStatusBadge';
import { useAuth } from '../../context/useAuth';
import usePagedList from '../../hooks/usePagedList';
import * as requestService from '../../services/requestService';
import { formatDate, formatDateOnly, formatMoney } from '../../utils/format';
import { REQUEST_STATUSES } from '../../utils/requestStatus';
import { ACCESS, ROLES } from '../../utils/roles';

export default function RequestsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const canCreate = ACCESS.requestCreate.includes(user.role);

  const [status, setStatus] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const list = usePagedList(requestService.getRequests, {
    filters: {
      status: status || undefined,
      from: from || undefined,
      to: to || undefined,
    },
    defaultSort: { by: 'created', descending: true },
  });
  const { data, loading, error, sort } = list;
  const filtered = list.hasSearch || status !== '' || from !== '' || to !== '';

  const clearFilters = () => {
    list.changeSearch('');
    setStatus('');
    setFrom('');
    setTo('');
  };

  const scopeText =
    user.role === ROLES.REQUESTER
      ? 'Your requests.'
      : user.role === ROLES.ADMIN
        ? 'All requests, including drafts.'
        : 'Submitted requests from everyone. Other people\'s drafts stay private.';

  const newButton = canCreate && (
    <Link to="/requests/new" className="btn btn-primary">
      New request
    </Link>
  );

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-1">
        <h2 className="h4 mb-0">Purchase Requests</h2>
        {newButton}
      </div>
      <p className="text-muted small mb-3">{scopeText}</p>

      <div className="card card-body mb-3">
        <div className="row g-2">
          <div className="col-12 col-md-4">
            <input
              type="search"
              className="form-control"
              placeholder="Search by number, title or requester..."
              value={list.searchText}
              onChange={(event) => list.changeSearch(event.target.value)}
            />
          </div>
          <div className="col-12 col-md-3">
            <select
              className="form-select"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                list.goToFirstPage();
              }}
            >
              <option value="">All statuses</option>
              {REQUEST_STATUSES.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </div>
          <div className="col-6 col-md-2">
            <input
              type="date"
              className="form-control"
              aria-label="Created from"
              title="Created from"
              value={from}
              onChange={(event) => {
                setFrom(event.target.value);
                list.goToFirstPage();
              }}
            />
          </div>
          <div className="col-6 col-md-2">
            <input
              type="date"
              className="form-control"
              aria-label="Created to"
              title="Created to"
              value={to}
              onChange={(event) => {
                setTo(event.target.value);
                list.goToFirstPage();
              }}
            />
          </div>
          <div className="col-12 col-md-1 d-grid">
            <button type="button" className="btn btn-outline-secondary" onClick={clearFilters} disabled={!filtered}>
              Clear
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger d-flex justify-content-between align-items-center">
          <span>{error}</span>
          <button type="button" className="btn btn-sm btn-outline-danger" onClick={list.reload}>
            Retry
          </button>
        </div>
      )}

      {loading && !data && <Loader text="Loading requests..." />}

      {data && data.items.length === 0 && (
        <EmptyState
          title="No requests found"
          message={filtered ? 'No request matches your search or filters.' : 'There are no purchase requests yet.'}
          action={filtered ? null : newButton}
        />
      )}

      {data && data.items.length > 0 && (
        <div style={{ opacity: loading ? 0.5 : 1 }}>
          {/* Desktop and tablet: table */}
          <div className="card d-none d-md-block">
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <SortHeader label="Number" column="number" sort={sort} onSort={list.toggleSort} />
                    <SortHeader label="Title" column="title" sort={sort} onSort={list.toggleSort} />
                    <th scope="col">Requester</th>
                    <SortHeader label="Status" column="status" sort={sort} onSort={list.toggleSort} />
                    <th scope="col" className="text-end">Items</th>
                    <SortHeader label="Amount" column="amount" sort={sort} onSort={list.toggleSort} />
                    <SortHeader label="Required by" column="required" sort={sort} onSort={list.toggleSort} />
                    <SortHeader label="Created" column="created" sort={sort} onSort={list.toggleSort} />
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((request) => (
                    <tr key={request.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/requests/${request.id}`)}>
                      <td className="text-nowrap">
                        <Link to={`/requests/${request.id}`} onClick={(event) => event.stopPropagation()}>
                          {request.requestNumber}
                        </Link>
                      </td>
                      <td>{request.title}</td>
                      <td>{request.requesterName}</td>
                      <td><RequestStatusBadge status={request.status} /></td>
                      <td className="text-end">{request.itemCount}</td>
                      <td className="text-end">{formatMoney(request.totalAmount)}</td>
                      <td>{formatDateOnly(request.requiredByDate)}</td>
                      <td>{formatDate(request.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Phones: one card per request */}
          <div className="d-md-none d-flex flex-column gap-2">
            {data.items.map((request) => (
              <Link key={request.id} to={`/requests/${request.id}`} className="card text-decoration-none text-body">
                <div className="card-body">
                  <div className="d-flex justify-content-between align-items-start">
                    <div>
                      <div className="fw-semibold">{request.requestNumber}</div>
                      <div>{request.title}</div>
                    </div>
                    <RequestStatusBadge status={request.status} />
                  </div>
                  <div className="small text-muted mt-2">
                    {request.requesterName} | {request.itemCount} item(s) | {formatDate(request.createdAt)}
                  </div>
                  <div className="fw-semibold mt-1">{formatMoney(request.totalAmount)}</div>
                </div>
              </Link>
            ))}
          </div>

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onChange={list.setPage}
          />
        </div>
      )}
    </div>
  );
}
