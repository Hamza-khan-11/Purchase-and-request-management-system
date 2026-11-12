import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import EmptyState from '../../components/common/EmptyState';
import Loader from '../../components/common/Loader';
import RequestStatusBadge from '../../components/requests/RequestStatusBadge';
import { getApiError } from '../../services/api';
import CreateOrderButton from '../../components/orders/CreateOrderButton';
import DecisionButtons from '../../components/requests/DecisionButtons';
import * as requestService from '../../services/requestService';
import { formatDate, formatDateOnly, formatDateTime, formatMoney } from '../../utils/format';

// What each button does, and what the confirmation dialog says
const ACTIONS = {
  submit: {
    title: 'Submit request',
    message: 'Submit this request for approval? You will not be able to edit it afterwards.',
    confirmText: 'Submit',
    success: 'Request submitted for approval.',
    danger: false,
    run: requestService.submitRequest,
  },
  cancel: {
    title: 'Cancel request',
    message: 'Cancel this request? This cannot be undone.',
    confirmText: 'Cancel request',
    success: 'Request cancelled.',
    danger: true,
    run: requestService.cancelRequest,
  },
  revise: {
    title: 'Revise request',
    message: 'Reopen this request as a draft so you can change it and submit it again?',
    confirmText: 'Reopen as draft',
    success: 'Request reopened as a draft.',
    danger: false,
    run: requestService.reviseRequest,
  },
  delete: {
    title: 'Delete draft',
    message: 'Delete this draft request? It will disappear from your lists.',
    confirmText: 'Delete',
    success: 'Draft deleted.',
    danger: true,
    run: requestService.deleteRequest,
  },
};

export default function RequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [reload, setReload] = useState(0);
  const [state, setState] = useState(null);   // the answer, remembered with the question it answered
  const [confirm, setConfirm] = useState(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    requestService
      .getRequest(id)
      .then((data) => {
        if (!cancelled) setState({ id, reload, data, error: null });
      })
      .catch((err) => {
        if (!cancelled) setState({ id, reload, data: null, error: getApiError(err) });
      });
    return () => {
      cancelled = true;
    };
  }, [id, reload]);

  const loading = !state || state.id !== id || state.reload !== reload;
  const request = state?.data ?? null;
  const error = loading ? null : state.error;

  const runAction = async () => {
    const action = ACTIONS[confirm];
    setBusy(true);
    try {
      await action.run(request.id);
      toast.success(action.success);
      setConfirm(null);
      if (confirm === 'delete') navigate('/requests', { replace: true });
      else setReload((count) => count + 1);
    } catch (err) {
      toast.error(getApiError(err).message);
      setConfirm(null);
      setReload((count) => count + 1);   // the request may have changed in the meantime
    } finally {
      setBusy(false);
    }
  };

  if (error) {
    return (
      <EmptyState
        title={error.status === 404 ? 'Request not found' : 'Could not load the request'}
        message={error.status === 404 ? 'It does not exist, or you are not allowed to see it.' : error.message}
        action={<Link to="/requests" className="btn btn-primary">Back to requests</Link>}
      />
    );
  }

  if (!request) {
    return <Loader text="Loading request..." />;
  }

  const permissions = request.permissions;
  const active = confirm ? ACTIONS[confirm] : null;

  return (
    <div style={{ opacity: loading ? 0.6 : 1 }}>
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
        <div>
          <h2 className="h4 mb-1">
            {request.requestNumber} <RequestStatusBadge status={request.status} />
          </h2>
          <div className="text-muted">{request.title}</div>
        </div>
        <div className="d-flex flex-wrap gap-2">
          <Link to="/requests" className="btn btn-outline-secondary">Back</Link>
          {permissions.canEdit && (
            <Link to={`/requests/${request.id}/edit`} className="btn btn-outline-primary">Edit</Link>
          )}
          {permissions.canSubmit && (
            <button type="button" className="btn btn-primary" onClick={() => setConfirm('submit')}>Submit</button>
          )}
          {permissions.canRevise && (
            <button type="button" className="btn btn-primary" onClick={() => setConfirm('revise')}>Revise</button>
          )}
          {permissions.canCancel && (
            <button type="button" className="btn btn-outline-warning" onClick={() => setConfirm('cancel')}>Cancel request</button>
          )}
          <CreateOrderButton request={request} />
          <DecisionButtons request={request} onDone={() => setReload((count) => count + 1)} />
          {permissions.canDelete && (
            <button type="button" className="btn btn-outline-danger" onClick={() => setConfirm('delete')}>Delete</button>
          )}
        </div>
      </div>

      <div className="card card-body mb-3">
        <div className="row g-3">
          <div className="col-6 col-md-3">
            <div className="small text-muted">Requester</div>
            <div>{request.requesterName}</div>
          </div>
          <div className="col-6 col-md-3">
            <div className="small text-muted">Created</div>
            <div>{formatDate(request.createdAt)}</div>
          </div>
          <div className="col-6 col-md-3">
            <div className="small text-muted">Required by</div>
            <div>{request.requiredByDate ? formatDateOnly(request.requiredByDate) : '-'}</div>
          </div>
          <div className="col-6 col-md-3">
            <div className="small text-muted">Submitted</div>
            <div>{request.submittedAt ? formatDateTime(request.submittedAt) : '-'}</div>
          </div>
          <div className="col-12">
            <div className="small text-muted">Justification</div>
            <div>{request.justification || '-'}</div>
          </div>
        </div>
      </div>

      <div className="card mb-3">
        <div className="card-header">Items</div>
        <div className="table-responsive">
          <table className="table align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>SKU</th>
                <th>Product</th>
                <th className="text-end">Quantity</th>
                <th className="text-end">Unit price</th>
                <th className="text-end">Line total</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {request.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.productSku}</td>
                  <td>{item.productName}</td>
                  <td className="text-end">{item.quantity} {item.unit}</td>
                  <td className="text-end">{formatMoney(item.unitPrice)}</td>
                  <td className="text-end">{formatMoney(item.lineTotal)}</td>
                  <td>{item.notes}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={4} className="text-end">Total</th>
                <th className="text-end">{formatMoney(request.totalAmount)}</th>
                <th />
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <div className="card">
        <div className="card-header">Approval history</div>
        {request.approvals.length === 0 ? (
          <div className="card-body text-muted">No decisions yet.</div>
        ) : (
          <ul className="list-group list-group-flush">
            {request.approvals.map((approval) => (
              <li className="list-group-item" key={approval.id}>
                <span className={`badge ${approval.decision === 'Approved' ? 'text-bg-success' : 'text-bg-danger'} me-2`}>
                  {approval.decision}
                </span>
                <strong>{approval.approverName}</strong>
                <span className="text-muted"> on {formatDateTime(approval.decidedAt)}</span>
                {approval.comments && <div className="mt-1">{approval.comments}</div>}
              </li>
            ))}
          </ul>
        )}
      </div>

      <ConfirmDialog
        open={Boolean(active)}
        title={active?.title ?? ''}
        message={active?.message ?? ''}
        confirmText={active?.confirmText}
        danger={active?.danger}
        busy={busy}
        onConfirm={runAction}
        onCancel={() => setConfirm(null)}
      />
    </div>
  );
}


