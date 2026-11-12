import { useState } from 'react';
import toast from 'react-hot-toast';
import FormField from '../../components/common/FormField';
import { getApiError } from '../../services/api';
import * as vendorService from '../../services/vendorService';
import { mapServerErrors } from '../../utils/forms';

const EMPTY = {
  code: '',
  name: '',
  contactPerson: '',
  email: '',
  phone: '',
  address: '',
  rating: '3',
  isActive: true,
};

// vendor = null means "add a new vendor", otherwise we edit that vendor
export default function VendorFormModal({ vendor, onClose, onSaved }) {
  const isEdit = Boolean(vendor);

  const [form, setForm] = useState(() =>
    vendor
      ? {
          code: vendor.code,
          name: vendor.name,
          contactPerson: vendor.contactPerson ?? '',
          email: vendor.email,
          phone: vendor.phone ?? '',
          address: vendor.address ?? '',
          rating: String(vendor.rating),
          isActive: vendor.isActive,
        }
      : EMPTY
  );
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  // Same rules as the API, so most mistakes are caught before a request is sent
  const validate = () => {
    const found = {};
    if (!form.code.trim()) found.code = 'Vendor code is required.';
    if (!form.name.trim()) found.name = 'Vendor name is required.';
    if (!form.email.trim()) found.email = 'Email is required.';
    else if (!/^\S+@\S+\.\S+$/.test(form.email.trim())) found.email = 'Enter a valid email address.';
    const rating = Number(form.rating);
    if (form.rating === '' || Number.isNaN(rating) || rating < 0 || rating > 5) {
      found.rating = 'Rating must be between 0 and 5.';
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
      const payload = { ...form, rating: Number(form.rating) };
      if (isEdit) {
        await vendorService.updateVendor(vendor.id, payload);
      } else {
        await vendorService.createVendor(payload);
      }
      toast.success(isEdit ? 'Vendor updated.' : 'Vendor created.');
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
              <h5 className="modal-title">{isEdit ? 'Edit vendor' : 'Add vendor'}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose} disabled={saving} />
            </div>

            <div className="modal-body">
              <div className="row">
                <div className="col-md-4">
                  <FormField label="Code" name="code" value={form.code} onChange={handleChange} error={errors.code} required maxLength={20} />
                </div>
                <div className="col-md-8">
                  <FormField label="Name" name="name" value={form.name} onChange={handleChange} error={errors.name} required maxLength={200} />
                </div>
                <div className="col-md-6">
                  <FormField label="Contact person" name="contactPerson" value={form.contactPerson} onChange={handleChange} error={errors.contactPerson} maxLength={150} />
                </div>
                <div className="col-md-6">
                  <FormField label="Email" name="email" type="email" value={form.email} onChange={handleChange} error={errors.email} required maxLength={150} />
                </div>
                <div className="col-md-6">
                  <FormField label="Phone" name="phone" value={form.phone} onChange={handleChange} error={errors.phone} maxLength={30} />
                </div>
                <div className="col-md-6">
                  <FormField label="Rating (0 to 5)" name="rating" type="number" min="0" max="5" step="0.1" value={form.rating} onChange={handleChange} error={errors.rating} required />
                </div>
                <div className="col-12">
                  <FormField label="Address" name="address" as="textarea" rows={2} value={form.address} onChange={handleChange} error={errors.address} maxLength={500} />
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
