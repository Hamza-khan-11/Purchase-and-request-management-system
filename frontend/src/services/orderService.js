import api from './api';

export async function getOrders(params) {
  const response = await api.get('/api/purchaseorders', { params });
  return response.data.data;
}

export async function getOrder(id) {
  const response = await api.get(`/api/purchaseorders/${id}`);
  return response.data.data;
}

// Active vendors that quote every item of the request, cheapest first
export async function getVendorOptions(requestId) {
  const response = await api.get('/api/purchaseorders/vendor-options', { params: { requestId } });
  return response.data.data;
}

export async function createOrder(payload) {
  const response = await api.post('/api/purchaseorders', payload);
  return response.data.data;
}

export async function sendOrder(id) {
  const response = await api.post(`/api/purchaseorders/${id}/send`);
  return response.data.data;
}

// deliveredDate is "YYYY-MM-DD"; empty means today
export async function deliverOrder(id, deliveredDate) {
  const response = await api.post(`/api/purchaseorders/${id}/deliver`, { deliveredDate: deliveredDate || null });
  return response.data.data;
}

export async function cancelOrder(id) {
  const response = await api.post(`/api/purchaseorders/${id}/cancel`);
  return response.data.data;
}
