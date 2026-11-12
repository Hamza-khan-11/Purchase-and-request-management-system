import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import EmptyState from '../../components/common/EmptyState';
import Loader from '../../components/common/Loader';
import Pagination from '../../components/common/Pagination';
import SortHeader from '../../components/common/SortHeader';
import { useAuth } from '../../context/useAuth';
import usePagedList from '../../hooks/usePagedList';
import { getApiError } from '../../services/api';
import * as productService from '../../services/productService';
import { formatDate, formatMoney } from '../../utils/format';
import { ACCESS } from '../../utils/roles';
import ProductFormModal from './ProductFormModal';

function StatusBadge({ active }) {
  return (
    <span className={`badge ${active ? 'text-bg-success' : 'text-bg-secondary'}`}>
      {active ? 'Active' : 'Inactive'}
    </span>
  );
}

export default function ProductsPage() {
  const { user } = useAuth();
  const canManage = ACCESS.masterData.includes(user.role);

  const [status, setStatus] = useState('all');
  const [category, setCategory] = useState('');
  const [categories, setCategories] = useState([]);
  const [categoriesVersion, setCategoriesVersion] = useState(0);

  const list = usePagedList(productService.getProducts, {
    filters: {
      category: category || undefined,
      isActive: status === 'all' ? undefined : status === 'active',
    },
    defaultSort: { by: 'name', descending: false },
  });
  const { data, loading, error, sort } = list;
  const filtered = list.hasSearch || status !== 'all' || category !== '';

  // undefined = form closed, null = add, object = edit that product
  const [formProduct, setFormProduct] = useState(undefined);
  const [deleting, setDeleting] = useState(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  // The category list for the filter and the form (refreshed after every change)
  useEffect(() => {
    let cancelled = false;
    productService
      .getCategories()
      .then((items) => {
        if (!cancelled) setCategories(items);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [categoriesVersion]);

  const refreshCategories = () => setCategoriesVersion((count) => count + 1);

  const handleSaved = () => {
    setFormProduct(undefined);
    list.reload();
    refreshCategories();
  };

  const confirmDelete = async () => {
    setDeleteBusy(true);
    try {
      await productService.deleteProduct(deleting.id);
      toast.success(`Product ${deleting.name} deleted.`);
      setDeleting(null);
      list.afterRemove();
      refreshCategories();
    } catch (err) {
      toast.error(getApiError(err).message);
      setDeleting(null);
    } finally {
      setDeleteBusy(false);
    }
  };

  const addButton = canManage && (
    <button type="button" className="btn btn-primary" onClick={() => setFormProduct(null)}>
      Add product
    </button>
  );

  return (
    <div>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h2 className="h4 mb-0">Products</h2>
        {addButton}
      </div>

      <div className="card card-body mb-3">
        <div className="row g-2">
          <div className="col-12 col-md-5">
            <input
              type="search"
              className="form-control"
              placeholder="Search by name, SKU or category..."
              value={list.searchText}
              onChange={(event) => list.changeSearch(event.target.value)}
            />
          </div>
          <div className="col-12 col-md-4">
            <select
              className="form-select"
              value={category}
              onChange={(event) => {
                setCategory(event.target.value);
                list.goToFirstPage();
              }}
            >
              <option value="">All categories</option>
              {categories.map((name) => (
                <option key={name} value={name}>
                  {name}
                </option>
              ))}
            </select>
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
          <button type="button" className="btn btn-sm btn-outline-danger" onClick={list.reload}>
            Retry
          </button>
        </div>
      )}

      {loading && !data && <Loader text="Loading products..." />}

      {data && data.items.length === 0 && (
        <EmptyState
          title="No products found"
          message={filtered ? 'No product matches your search or filters.' : 'No products have been added yet.'}
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
                    <SortHeader label="SKU" column="sku" sort={sort} onSort={list.toggleSort} />
                    <SortHeader label="Name" column="name" sort={sort} onSort={list.toggleSort} />
                    <SortHeader label="Category" column="category" sort={sort} onSort={list.toggleSort} />
                    <th scope="col">Unit</th>
                    <SortHeader label="Price" column="price" sort={sort} onSort={list.toggleSort} />
                    <th scope="col">Status</th>
                    <th scope="col" className="d-none d-lg-table-cell">Created</th>
                    {canManage && <th scope="col" className="text-end">Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((product) => (
                    <tr key={product.id}>
                      <td>{product.sku}</td>
                      <td>{product.name}</td>
                      <td>{product.category}</td>
                      <td>{product.unit}</td>
                      <td>{formatMoney(product.standardPrice)}</td>
                      <td><StatusBadge active={product.isActive} /></td>
                      <td className="d-none d-lg-table-cell">{formatDate(product.createdAt)}</td>
                      {canManage && (
                        <td className="text-end text-nowrap">
                          <button type="button" className="btn btn-sm btn-outline-primary me-2" onClick={() => setFormProduct(product)}>
                            Edit
                          </button>
                          <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setDeleting(product)}>
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

          {/* Phones: one card per product */}
          <div className="d-md-none d-flex flex-column gap-2">
            {data.items.map((product) => (
              <div className="card" key={product.id}>
                <div className="card-body">
                  <div className="d-flex justify-content-between align-items-start">
                    <div>
                      <div className="fw-semibold">{product.name}</div>
                      <div className="small text-muted">{product.sku} | {product.category}</div>
                    </div>
                    <StatusBadge active={product.isActive} />
                  </div>
                  <div className="small mt-2">
                    {formatMoney(product.standardPrice)} per {product.unit}
                  </div>
                  {canManage && (
                    <div className="d-flex gap-2 mt-3">
                      <button type="button" className="btn btn-sm btn-outline-primary" onClick={() => setFormProduct(product)}>
                        Edit
                      </button>
                      <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setDeleting(product)}>
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
            onChange={list.setPage}
          />
        </div>
      )}

      {formProduct !== undefined && (
        <ProductFormModal
          key={formProduct?.id ?? 'new'}
          product={formProduct}
          categories={categories}
          onClose={() => setFormProduct(undefined)}
          onSaved={handleSaved}
        />
      )}

      <ConfirmDialog
        open={Boolean(deleting)}
        title="Delete product"
        message={
          deleting
            ? `Delete ${deleting.name} (${deleting.sku})? Products used in open purchase requests cannot be deleted.`
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
