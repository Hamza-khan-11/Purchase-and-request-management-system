import api from './api';

// Returns { items, page, pageSize, totalCount, totalPages, hasPrevious, hasNext }
export async function getVendors(params) {
  const response = await api.get('/api/vendors', { params });
  return response.data.data;
}

export async function createVendor(vendor) {
  const response = await api.post('/api/vendors', vendor);
  return response.data.data;
}

export async function updateVendor(id, vendor) {
  const response = await api.put(`/api/vendors/${id}`, vendor);
  return response.data.data;
}

export async function deleteVendor(id) {
  await api.delete(`/api/vendors/${id}`);
}
