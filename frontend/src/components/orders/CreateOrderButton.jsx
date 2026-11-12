import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { getApiError } from '../../services/api';
import * as orderService from '../../services/orderService';
import { addDays, utcToday } from '../../utils/dates';
import { formatMoney } from '../../utils/format';
import { mapServerErrors } from '../../utils/forms';

function CreateOrderDialog({ request, onClose }) {
  const navigate = useNavigate();
  const [options, setOptions] = useState(null);   // null while loading
  const [loadError, setLoadError] = useState('');
  const [vendorId, setVendorId] = useState('');
  const [promised, setPromised] = useState('');
  const [errors, setErrors] = useState({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    orderService
      .getVendorOptions(request.id)
      .then((items) => {
        if (!cancelled) setOptions(items);
      })
      .catch((error) => {
        if (!cancelled) setLoadError(getApiError(error).message);
      });
    return () => {
      cancelled = true;
    };
  }, [request.id]);

  // Choosing a vendor suggests a delivery date from its lead time
  const choose = (option) => {
    setVendorId(String(option.vendorId));
    setPromised(addDays(utcToday(), option.leadTimeDays));
    setErrors({});
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const found = {};
    if (!vendorId) found.vendorId = 'Select a vendor.';
    if (!promised) found.promisedDeliveryDate = 'Promised delivery date is required.';
    else if (promised < utcToday()) found.promisedDeliveryDate = 'The promised delivery date cannot be in the past.';
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setSaving(true);
    try {
      const order = await orderService.createOrder({
        purchaseRequestId: request.id,
        vendorId: Number(vendorId),
        promisedDeliveryDate: promised,
      });
      toast.success(`Purchase order ${order.poNumber} created.`);
      navigate(`/orders/${order.id}`);
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
              <h5 className="modal-title">Create purchase order for {request.requestNumber}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose} disabled={saving} />
            </div>

            <div className="modal-body">
              <p className="text-muted small">
                Only active vendors that quote every item of this request are listed, cheapest first. The request
                was estimated at {formatMoney(request.totalAmount)}.
              </p>

              {loadError && <div className="alert alert-danger">{loadError}</div>}
              {!options && !loadError && <div className="text-muted">Loading vendors...</div>}

              {options && options.length === 0 && (
                <div className="alert alert-warning">
                  No active vendor has a price quote for every item of this request, so an order cannot be created yet.
                </div>
              )}

              {options && options.length > 0 && (
                <div className="table-responsive">
                  <table className="table table-hover align-middle">
                    <thead className="table-light">
                      <tr>
                        <th scope="col" />
                        <th scope="col">Vendor</th>
                        <th scope="col" className="text-end">Rating</th>
                        <th scope="col" className="text-end">Total price</th>
                        <th scope="col" className="text-end">Versus estimate</th>
                        <th scope="col" className="text-end">Lead time</th>
                      </tr>
                    </thead>
                    <tbody>
                      {options.map((option, index) => {
                        const difference = option.totalPrice - request.totalAmount;
                        return (
                          <tr key={option.vendorId} style={{ cursor: 'pointer' }} onClick={() => choose(option)}>
                            <td>
                              <input
                                type="radio"
                                name="vendor"
                                className="form-check-input"
                                checked={vendorId === String(option.vendorId)}
                                onChange={() => choose(option)}
                                aria-label={`Choose ${option.vendorName}`}
                              />
                            </td>
                            <td>
                              {option.vendorName} <span className="text-muted small">{option.vendorCode}</span>
                              {index === 0 && <span className="badge text-bg-success ms-2">Cheapest</span>}
                            </td>
                            <td className="text-end">{Number(option.rating).toFixed(1)}</td>
                            <td className="text-end">{formatMoney(option.totalPrice)}</td>
                            <td className={`text-end ${difference < 0 ? 'text-success' : difference > 0 ? 'text-danger' : ''}`}>
                              {difference > 0 ? '+' : ''}{formatMoney(difference)}
                            </td>
                            <td className="text-end">{option.leadTimeDays} day(s)</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
              {errors.vendorId && <div className="text-danger small mb-2">{errors.vendorId}</div>}

              {options && options.length > 0 && (
                <div className="row">
                  <div className="col-md-5">
                    <label htmlFor="promised" className="form-label">Promised delivery date</label>
                    <input
                      id="promised"
                      type="date"
                      className={`form-control${errors.promisedDeliveryDate ? ' is-invalid' : ''}`}
                      min={utcToday()}
                      value={promised}
                      onChange={(event) => setPromised(event.target.value)}
                    />
                    {errors.promisedDeliveryDate && <div className="invalid-feedback">{errors.promisedDeliveryDate}</div>}
                  </div>
                </div>
              )}
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose} disabled={saving}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={saving || !options || options.length === 0}>
                {saving ? 'Creating...' : 'Create order'}
              </button>
            </div>
          </form>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  );
}

// The button on the request page. It only appears when the server says canCreateOrder.
export default function CreateOrderButton({ request }) {
  const [open, setOpen] = useState(false);

  if (!request.permissions.canCreateOrder) return null;

  return (
    <>
      <button type="button" className="btn btn-primary" onClick={() => setOpen(true)}>
        Create purchase order
      </button>
      {open && <CreateOrderDialog request={request} onClose={() => setOpen(false)} />}
    </>
  );
}
