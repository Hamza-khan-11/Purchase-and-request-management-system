import api from './api';

export async function login(email, password) {
  const response = await api.post('/api/auth/login', { email, password });
  return response.data.data; // { token, expiresAt, user }
}

export function logout() {
  return api.post('/api/auth/logout');
}
