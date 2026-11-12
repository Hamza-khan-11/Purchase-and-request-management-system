import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import FormField from '../../components/common/FormField';
import Loader from '../../components/common/Loader';
import { getApiError } from '../../services/api';
import * as productService from '../../services/productService';
import * as requestService from '../../services/requestService';
import { formatMoney } from '../../utils/format';
import { mapServerErrors } from '../../utils/forms';

const EMPTY_FORM = {
  title: '',
  justification: '',
  requiredByDate: '',
  items: [{ key: 1, productId: '', quantity: '1', notes: '' }],
};

export default function RequestFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();

  const [products, setProducts] = useState(null);
  const [loadError, setLoadError] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);
  const nextKey = useRef(1);   // gives every item row its own identity

  // Load the product list (and, when editing, the request itself)
  useEffect(() => {
    let cancelled = false;

    Promise.all([
      productService.getProductOptions(),
      isEdit ? requestService.getRequest(id) : Promise.resolve(null),
    ])
      .then(([options, request]) => {
        if (cancelled) return;

        if (request) {
          if (!request.permissions.canEdit) {
            toast.error('Only your own draft requests can be edited.');
            navigate(`/requests/${id}`, { replace: true });
            return;
          }
          const items = request.items.map((item) => {
            nextKey.current += 1;
            return {
              key: nextKey.current,
              productId: String(item.productId),
              quantity: String(item.quantity),
              notes: item.notes ?? '',
            };
          });
          setForm({
            title: request.title,
            justification: request.justification ?? '',
            requiredByDate: request.requiredByDate ? request.requiredByDate.slice(0, 10) : '',
            items,
          });
        }
        setProducts(options);
      })
      .catch((error) => {
        if (!cancelled) setLoadError(getApiError(error).message);
      });

    return () => {
      cancelled = true;
    };
  }, [id, isEdit, navigate]);

  const productsById = useMemo(
    () => new Map((products ?? []).map((product) => [String(product.id), product])),
    [products]
  );

  const setField = (event) => {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
  };

  const updateItem = (key, changes) => {
    setForm((current) => ({
      ...current,
      items: current.items.map((item) => (item.key === key ? { ...item, ...changes } : item)),
    }));
  };

  const addItem = () => {
    const key = nextKey.current + 1;
    nextKey.current = key;
    setForm((current) => ({
      ...current,
      items: [...current.items, { key, productId: '', quantity: '1', notes: '' }],
    }));
  };

  const removeItem = (key) => {
    setForm((current) => ({ ...current, items: current.items.filter((item) => item.key !== key) }));
  };

  // Preview only. The server calculates the real prices and totals.
  const lineTotal = (item) => {
    const product = productsById.get(item.productId);
    const quantity = Number(item.quantity);
    return product && Number.isInteger(quantity) && quantity > 0 ? product.standardPrice * quantity : 0;
  };
  const grandTotal = form.items.reduce((sum, item) => sum + lineTotal(item), 0);
  const usedProductIds = new Set(form.items.map((item) => item.productId).filter(Boolean));

  const validate = () => {
    const found = {};
    if (!form.title.trim()) found.title = 'Title is required.';
    if (form.items.length === 0) found.items = 'Add at least one item.';

    const seen = new Set();
    form.items.forEach((item, index) => {
      if (!item.productId) {
        found[`items[${index}].ProductId`] = 'Select a product.';
      } else if (seen.has(item.productId)) {
        found.items = 'Each product can only be added once.';
      } else {
        seen.add(item.productId);
      }
      const quantity = Number(item.quantity);
      if (!Number.isInteger(quantity) || quantity < 1 || quantity > 10000) {
        found[`items[${index}].Quantity`] = 'Enter a whole number from 1 to 10000.';
      }
    });
    return found;
  };

  const save = async (submitAfter) => {
    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0) {
      toast.error('Please fix the highlighted fields.');
      return;
    }

    setSaving(true);
    try {
      const payload = {
        title: form.title.trim(),
        justification: form.justification.trim() || null,
        requiredByDate: form.requiredByDate || null,
        items: form.items.map((item) => ({
          productId: Number(item.productId),
          quantity: Number(item.quantity),
          notes: item.notes.trim() || null,
        })),
      };

      const saved = isEdit
        ? await requestService.updateRequest(id, payload)
        : await requestService.createRequest(payload);

      if (submitAfter) {
        try {
          await requestService.submitRequest(saved.id);
          toast.success('Request submitted for approval.');
        } catch (submitError) {
          toast.error(`${getApiError(submitError).message} The draft was saved.`);
        }
      } else {
        toast.success(isEdit ? 'Draft updated.' : 'Draft saved.');
      }
      navigate(`/requests/${saved.id}`);
    } catch (error) {
      const apiError = getApiError(error);
      const serverErrors = mapServerErrors(apiError.errors);
      setErrors(serverErrors);
      toast.error(Object.keys(serverErrors).length > 0 ? 'Please fix the highlighted fields.' : apiError.message);
    } finally {
      setSaving(false);
    }
  };

  if (loadError) {
    return <div className="alert alert-danger">{loadError}</div>;
  }
  if (!products) {
    return <Loader text="Loading form..." />;
  }

  return (
    <div>
      <h2 className="h4 mb-3">{isEdit ? 'Edit purchase request' : 'New purchase request'}</h2>

      <div className="card card-body mb-3">
        <div className="row">
          <div className="col-md-8">
            <FormField label="Title" name="title" value={form.title} onChange={setField} error={errors.title} required maxLength={200} />
          </div>
          <div className="col-md-4">
            <FormField label="Required by" name="requiredByDate" type="date" value={form.requiredByDate} onChange={setField} error={errors.requiredByDate} />
          </div>
          <div className="col-12">
            <FormField label="Justification" name="justification" as="textarea" rows={2} value={form.justification} onChange={setField} error={errors.justification} maxLength={1000} />
          </div>
        </div>
      </div>

      <div className="card card-body mb-3">
        <div className="d-flex justify-content-between align-items-center mb-2">
          <h3 className="h6 mb-0">Items</h3>
          <button type="button" className="btn btn-sm btn-outline-primary" onClick={addItem}>
            Add item
          </button>
        </div>
        {errors.items && <div className="text-danger small mb-2">{errors.items}</div>}

        {form.items.map((item, index) => {
          const product = productsById.get(item.productId);
          const productError = errors[`items[${index}].ProductId`];
          const quantityError = errors[`items[${index}].Quantity`];

          return (
            <div className="border rounded p-2 mb-2" key={item.key}>
              <div className="row g-2 align-items-start">
                <div className="col-12 col-md-5">
                  <label className="form-label small mb-1">Product</label>
                  <select
                    className={`form-select${productError ? ' is-invalid' : ''}`}
                    value={item.productId}
                    onChange={(event) => updateItem(item.key, { productId: event.target.value })}
                  >
                    <option value="">Select a product...</option>
                    {products.map((option) => (
                      <option
                        key={option.id}
                        value={option.id}
                        disabled={usedProductIds.has(String(option.id)) && String(option.id) !== item.productId}
                      >
                        {option.sku} - {option.name}
                      </option>
                    ))}
                  </select>
                  {productError && <div className="invalid-feedback">{productError}</div>}
                </div>

                <div className="col-6 col-md-2">
                  <label className="form-label small mb-1">Quantity</label>
                  <input
                    type="number"
                    min="1"
                    max="10000"
                    className={`form-control${quantityError ? ' is-invalid' : ''}`}
                    value={item.quantity}
                    onChange={(event) => updateItem(item.key, { quantity: event.target.value })}
                  />
                  {quantityError && <div className="invalid-feedback">{quantityError}</div>}
                </div>

                <div className="col-6 col-md-2">
                  <label className="form-label small mb-1">Unit price</label>
                  <div className="form-control-plaintext">
                    {product ? `${formatMoney(product.standardPrice)} / ${product.unit}` : '-'}
                  </div>
                </div>

                <div className="col-8 col-md-2">
                  <label className="form-label small mb-1">Line total</label>
                  <div className="form-control-plaintext fw-semibold">{formatMoney(lineTotal(item))}</div>
                </div>

                <div className="col-4 col-md-1 text-end">
                  <label className="form-label small mb-1 d-none d-md-block">&nbsp;</label>
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={() => removeItem(item.key)}
                    disabled={form.items.length === 1}
                    aria-label="Remove item"
                  >
                    Remove
                  </button>
                </div>

                <div className="col-12">
                  <input
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="Notes for this item (optional)"
                    maxLength={500}
                    value={item.notes}
                    onChange={(event) => updateItem(item.key, { notes: event.target.value })}
                  />
                </div>
              </div>
            </div>
          );
        })}

        <div className="text-end mt-2">
          <span className="text-muted me-2">Estimated total</span>
          <span className="h5 mb-0">{formatMoney(grandTotal)}</span>
        </div>
      </div>

      <div className="d-flex flex-wrap gap-2 justify-content-end">
        <Link to={isEdit ? `/requests/${id}` : '/requests'} className="btn btn-outline-secondary">
          Cancel
        </Link>
        <button type="button" className="btn btn-outline-primary" disabled={saving} onClick={() => save(false)}>
          {saving ? 'Saving...' : 'Save draft'}
        </button>
        <button type="button" className="btn btn-primary" disabled={saving} onClick={() => save(true)}>
          Save and submit
        </button>
      </div>
    </div>
  );
}
