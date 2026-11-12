import api from './api';

export async function getProducts(params) {
  const response = await api.get('/api/products', { params });
  return response.data.data;
}

// All active products for dropdowns (the API allows at most 100 per page)
export async function getProductOptions() {
  const response = await api.get('/api/products', {
    params: { pageSize: 100, isActive: true, sortBy: 'name' },
  });
  return response.data.data.items;
}

export async function getCategories() {
  const response = await api.get('/api/products/categories');
  return response.data.data;
}

export async function createProduct(product) {
  const response = await api.post('/api/products', product);
  return response.data.data;
}

export async function updateProduct(id, product) {
  const response = await api.put(`/api/products/${id}`, product);
  return response.data.data;
}

export async function deleteProduct(id) {
  await api.delete(`/api/products/${id}`);
}
