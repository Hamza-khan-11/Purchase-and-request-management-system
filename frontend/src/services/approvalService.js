import api from './api';

// comments is optional for approve and required for reject
export async function approveRequest(id, comments) {
  const response = await api.post(`/api/approvals/${id}/approve`, { comments });
  return response.data.data;
}

export async function rejectRequest(id, comments) {
  const response = await api.post(`/api/approvals/${id}/reject`, { comments });
  return response.data.data;
}
