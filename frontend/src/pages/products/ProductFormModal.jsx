import { useState } from 'react';
import toast from 'react-hot-toast';
import FormField from '../../components/common/FormField';
import { getApiError } from '../../services/api';
import * as productService from '../../services/productService';
import { mapServerErrors } from '../../utils/forms';

const EMPTY = {
  sku: '',
  name: '',
  category: '',
  unit: 'Piece',
  standardPrice: '0',
  isActive: true,
};

// product = null means "add a new product", otherwise we edit that product
export default function ProductFormModal({ product, categories, onClose, onSaved }) {
  const isEdit = Boolean(product);

  const [form, setForm] = useState(() =>
    product
      ? {
          sku: product.sku,
          name: product.name,
          category: product.category,
          unit: product.unit,
          standardPrice: String(product.standardPrice),
          isActive: product.isActive,
        }
      : EMPTY
  );
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const validate = () => {
    const found = {};
    if (!form.sku.trim()) found.sku = 'SKU is required.';
    if (!form.name.trim()) found.name = 'Product name is required.';
    if (!form.category.trim()) found.category = 'Category is required.';
    if (!form.unit.trim()) found.unit = 'Unit is required.';
    const price = Number(form.standardPrice);
    if (form.standardPrice === '' || Number.isNaN(price) || price < 0) {
      found.standardPrice = 'Price must be zero or more.';
    }
    return found;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setSaving(true);
    try {
      const payload = { ...form, standardPrice: Number(form.standardPrice) };
      if (isEdit) {
        await productService.updateProduct(product.id, payload);
      } else {
        await productService.createProduct(payload);
      }
      toast.success(isEdit ? 'Product updated.' : 'Product created.');
      onSaved();
    } catch (error) {
      const apiError = getApiError(error);
      const serverErrors = mapServerErrors(apiError.errors);
      setErrors(serverErrors);
      toast.error(Object.keys(serverErrors).length > 0 ? 'Please fix the highlighted fields.' : apiError.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true">
        <div className="modal-dialog modal-lg modal-dialog-scrollable">
          <form className="modal-content" onSubmit={handleSubmit} noValidate>
            <div className="modal-header">
              <h5 className="modal-title">{isEdit ? 'Edit product' : 'Add product'}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose} disabled={saving} />
            </div>

            <div className="modal-body">
              <div className="row">
                <div className="col-md-4">
                  <FormField label="SKU" name="sku" value={form.sku} onChange={handleChange} error={errors.sku} required maxLength={50} />
                </div>
                <div className="col-md-8">
                  <FormField label="Name" name="name" value={form.name} onChange={handleChange} error={errors.name} required maxLength={200} />
                </div>
                <div className="col-md-6">
                  <FormField label="Category" name="category" list="product-categories" value={form.category} onChange={handleChange} error={errors.category} required maxLength={100} />
                </div>
                <div className="col-md-3">
                  <FormField label="Unit" name="unit" list="product-units" value={form.unit} onChange={handleChange} error={errors.unit} required maxLength={30} />
                </div>
                <div className="col-md-3">
                  <FormField label="Standard price" name="standardPrice" type="number" min="0" step="0.01" value={form.standardPrice} onChange={handleChange} error={errors.standardPrice} required />
                </div>
                <div className="col-12">
                  <div className="form-check form-switch">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="field-isActive"
                      name="isActive"
                      checked={form.isActive}
                      onChange={handleChange}
                    />
                    <label className="form-check-label" htmlFor="field-isActive">
                      Active
                    </label>
                  </div>
                </div>
              </div>

              <datalist id="product-categories">
                {categories.map((name) => (
                  <option key={name} value={name} />
                ))}
              </datalist>
              <datalist id="product-units">
                <option value="Piece" />
                <option value="Box" />
                <option value="Ream" />
                <option value="Pack" />
                <option value="Kg" />
                <option value="Litre" />
              </datalist>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose} disabled={saving}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </form>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  );
}
