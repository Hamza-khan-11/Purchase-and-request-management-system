import { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import EmptyState from '../../components/common/EmptyState';
import Loader from '../../components/common/Loader';
import Pagination from '../../components/common/Pagination';
import SortHeader from '../../components/common/SortHeader';
import { useAuth } from '../../context/useAuth';
import useDebounce from '../../hooks/useDebounce';
import { getApiError } from '../../services/api';
import * as vendorService from '../../services/vendorService';
import { formatDate } from '../../utils/format';
import { ACCESS } from '../../utils/roles';
import VendorFormModal from './VendorFormModal';

const PAGE_SIZE = 10;

function StatusBadge({ active }) {
  return (
    <span className={`badge ${active ? 'text-bg-success' : 'text-bg-secondary'}`}>
      {active ? 'Active' : 'Inactive'}
    </span>
  );
}

export default function VendorsPage() {
  const { user } = useAuth();
  const canManage = ACCESS.masterData.includes(user.role);

  // What the user chose on screen
  const [searchText, setSearchText] = useState('');
  const search = useDebounce(searchText, 400);
  const [status, setStatus] = useState('all');
  const [sort, setSort] = useState({ by: 'name', descending: false });
  const [page, setPage] = useState(1);
  const [reload, setReload] = useState(0);

  // What the server answered, remembered together with the question it answered
  const [result, setResult] = useState(null);

  // undefined = form closed, null = add, object = edit that vendor
  const [formVendor, setFormVendor] = useState(undefined);
  const [deleting, setDeleting] = useState(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  const params = useMemo(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      search: search.trim() || undefined,
      sortBy: sort.by,
      descending: sort.descending,
      isActive: status === 'all' ? undefined : status === 'active',
    }),
    [page, search, sort, status]
  );

  useEffect(() => {
    let cancelled = false;
    vendorService
      .getVendors(params)
      .then((data) => {
        if (!cancelled) setResult({ params, reload, data, error: null });
      })
      .catch((error) => {
        if (!cancelled) setResult({ params, reload, data: null, error: getApiError(error).message });
      });
    return () => {
      cancelled = true;
    };
  }, [params, reload]);

  const loading = !result || result.params !== params || result.reload !== reload;
  const data = result?.data ?? null;
  const error = loading ? null : result.error;
  const filtered = Boolean(search.trim()) || status !== 'all';

  const handleSort = (column) => {
    setSort((current) =>
      current.by === column
        ? { by: column, descending: !current.descending }
        : { by: column, descending: false }
    );
    setPage(1);
  };

  const handleSaved = () => {
    setFormVendor(undefined);
    setReload((count) => count + 1);
  };

  const confirmDelete = async () => {
    setDeleteBusy(true);
    try {
      await vendorService.deleteVendor(deleting.id);
      toast.success(`Vendor ${deleting.name} deleted.`);
      setDeleting(null);
      // If that was the last row on this page, go back one page
      if (data && data.items.length === 1 && page > 1) setPage(page - 1);
      else setReload((count) => count + 1);
    } catch (err) {
      toast.error(getApiError(err).message);
      setDeleting(null);
    } finally {
      setDeleteBusy(false);
    }
  };

  const addButton = canManage && (
    <button type="button" className="btn btn-primary" onClick={() => setFormVendor(null)}>
      Add vendor
    </button>
  );

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h2 className="h4 mb-0">Vendors</h2>
        {addButton}
      </div>

      <div className="card card-body mb-3">
        <div className="row g-2">
          <div className="col-12 col-md-8">
            <input
              type="search"
              className="form-control"
              placeholder="Search by name, code, email or contact person..."
              value={searchText}
              onChange={(event) => {
                setSearchText(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="col-12 col-md-4">
            <select
              className="form-select"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                setPage(1);
              }}
            >
              <option value="all">All statuses</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger d-flex justify-content-between align-items-center">
          <span>{error}</span>
          <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setReload((count) => count + 1)}>
            Retry
          </button>
        </div>
      )}

      {loading && !data && <Loader text="Loading vendors..." />}

      {data && data.items.length === 0 && (
        <EmptyState
          title="No vendors found"
          message={filtered ? 'No vendor matches your search or filter.' : 'No vendors have been added yet.'}
          action={filtered ? null : addButton}
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
                    <SortHeader label="Code" column="code" sort={sort} onSort={handleSort} />
                    <SortHeader label="Name" column="name" sort={sort} onSort={handleSort} />
                    <th scope="col">Contact</th>
                    <SortHeader label="Email" column="email" sort={sort} onSort={handleSort} />
                    <SortHeader label="Rating" column="rating" sort={sort} onSort={handleSort} />
                    <th scope="col">Status</th>
                    <th scope="col" className="d-none d-lg-table-cell">Created</th>
                    {canManage && <th scope="col" className="text-end">Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((vendor) => (
                    <tr key={vendor.id}>
                      <td>{vendor.code}</td>
                      <td>{vendor.name}</td>
                      <td>{vendor.contactPerson}</td>
                      <td>{vendor.email}</td>
                      <td>{Number(vendor.rating).toFixed(1)}</td>
                      <td><StatusBadge active={vendor.isActive} /></td>
                      <td className="d-none d-lg-table-cell">{formatDate(vendor.createdAt)}</td>
                      {canManage && (
                        <td className="text-end text-nowrap">
                          <button type="button" className="btn btn-sm btn-outline-primary me-2" onClick={() => setFormVendor(vendor)}>
                            Edit
                          </button>
                          <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setDeleting(vendor)}>
                            Delete
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Phones: one card per vendor */}
          <div className="d-md-none d-flex flex-column gap-2">
            {data.items.map((vendor) => (
              <div className="card" key={vendor.id}>
                <div className="card-body">
                  <div className="d-flex justify-content-between align-items-start">
                    <div>
                      <div className="fw-semibold">{vendor.name}</div>
                      <div className="small text-muted">{vendor.code}</div>
                    </div>
                    <StatusBadge active={vendor.isActive} />
                  </div>
                  <div className="small mt-2">{vendor.email}</div>
                  {vendor.contactPerson && <div className="small text-muted">{vendor.contactPerson}</div>}
                  <div className="small mt-1">
                    Rating: {Number(vendor.rating).toFixed(1)} | Added {formatDate(vendor.createdAt)}
                  </div>
                  {canManage && (
                    <div className="d-flex gap-2 mt-3">
                      <button type="button" className="btn btn-sm btn-outline-primary" onClick={() => setFormVendor(vendor)}>
                        Edit
                      </button>
                      <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setDeleting(vendor)}>
                        Delete
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            pageSize={data.pageSize}
            onChange={setPage}
          />
        </div>
      )}

      {formVendor !== undefined && (
        <VendorFormModal
          key={formVendor?.id ?? 'new'}
          vendor={formVendor}
          onClose={() => setFormVendor(undefined)}
          onSaved={handleSaved}
        />
      )}

      <ConfirmDialog
        open={Boolean(deleting)}
        title="Delete vendor"
        message={
          deleting
            ? `Delete ${deleting.name} (${deleting.code})? It will be removed from the lists. Vendors with open purchase orders cannot be deleted.`
            : ''
        }
        confirmText="Delete"
        danger
        busy={deleteBusy}
        onConfirm={confirmDelete}
        onCancel={() => setDeleting(null)}
      />
    </div>
  );
}
