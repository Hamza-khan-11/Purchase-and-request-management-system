import api from './api';

export async function getRequests(params) {
  const response = await api.get('/api/purchaserequests', { params });
  return response.data.data;
}

export async function getRequest(id) {
  const response = await api.get(`/api/purchaserequests/${id}`);
  return response.data.data;
}

export async function createRequest(payload) {
  const response = await api.post('/api/purchaserequests', payload);
  return response.data.data;
}

export async function updateRequest(id, payload) {
  const response = await api.put(`/api/purchaserequests/${id}`, payload);
  return response.data.data;
}

export async function deleteRequest(id) {
  await api.delete(`/api/purchaserequests/${id}`);
}

export async function submitRequest(id) {
  const response = await api.post(`/api/purchaserequests/${id}/submit`);
  return response.data.data;
}

export async function cancelRequest(id) {
  const response = await api.post(`/api/purchaserequests/${id}/cancel`);
  return response.data.data;
}

export async function reviseRequest(id) {
  const response = await api.post(`/api/purchaserequests/${id}/revise`);
  return response.data.data;
}
