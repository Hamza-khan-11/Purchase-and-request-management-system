import { useState } from 'react';
import toast from 'react-hot-toast';
import { getApiError } from '../../services/api';
import * as approvalService from '../../services/approvalService';
import { mapServerErrors } from '../../utils/forms';

// The dialog where the manager types a comment
function DecisionDialog({ mode, busy, serverError, onSubmit, onCancel }) {
  const [comments, setComments] = useState('');
  const [error, setError] = useState('');
  const isReject = mode === 'reject';

  const handleSubmit = (event) => {
    event.preventDefault();
    if (isReject && !comments.trim()) {
      setError('A comment is required when rejecting a request.');
      return;
    }
    setError('');
    onSubmit(comments.trim());
  };

  const shownError = error || serverError;

  return (
    <>
      <div className="modal d-block" tabIndex={-1} role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered">
          <form className="modal-content" onSubmit={handleSubmit} noValidate>
            <div className="modal-header">
              <h5 className="modal-title">{isReject ? 'Reject request' : 'Approve request'}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onCancel} disabled={busy} />
            </div>
            <div className="modal-body">
              <label htmlFor="decision-comments" className="form-label">
                {isReject ? 'Reason (required)' : 'Comment (optional)'}
              </label>
              <textarea
                id="decision-comments"
                className={`form-control${shownError ? ' is-invalid' : ''}`}
                rows={3}
                maxLength={1000}
                value={comments}
                onChange={(event) => setComments(event.target.value)}
              />
              {shownError && <div className="invalid-feedback">{shownError}</div>}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onCancel} disabled={busy}>
                Cancel
              </button>
              <button type="submit" className={`btn ${isReject ? 'btn-danger' : 'btn-success'}`} disabled={busy}>
                {busy ? 'Please wait...' : isReject ? 'Reject' : 'Approve'}
              </button>
            </div>
          </form>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </>
  );
}

// The Approve and Reject buttons. They only appear when the server says canDecide.
export default function DecisionButtons({ request, onDone }) {
  const [mode, setMode] = useState(null);   // 'approve', 'reject' or null
  const [busy, setBusy] = useState(false);
  const [serverError, setServerError] = useState('');

  if (!request.permissions.canDecide) return null;

  const close = () => {
    setMode(null);
    setServerError('');
  };

  const submit = async (comments) => {
    setBusy(true);
    setServerError('');
    try {
      if (mode === 'approve') await approvalService.approveRequest(request.id, comments);
      else await approvalService.rejectRequest(request.id, comments);
      toast.success(mode === 'approve' ? 'Request approved.' : 'Request rejected.');
      close();
      onDone();
    } catch (err) {
      const apiError = getApiError(err);
      const fieldErrors = mapServerErrors(apiError.errors);
      if (fieldErrors.comments) {
        setServerError(fieldErrors.comments);   // stay open so the manager can fix it
      } else {
        toast.error(apiError.message);          // for example: someone else decided first
        close();
        onDone();                               // refresh the page with the latest state
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <button type="button" className="btn btn-success" onClick={() => setMode('approve')}>
        Approve
      </button>
      <button type="button" className="btn btn-danger" onClick={() => setMode('reject')}>
        Reject
      </button>
      {mode && (
        <DecisionDialog key={mode} mode={mode} busy={busy} serverError={serverError} onSubmit={submit} onCancel={close} />
      )}
    </>
  );
}
