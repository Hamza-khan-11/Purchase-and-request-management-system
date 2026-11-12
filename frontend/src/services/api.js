import axios from 'axios';

const TOKEN_KEY = 'pa_token';

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Attach the token to every request
api.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// If the API says 401 (not logged in / expired), tell the app to log out
let onUnauthorized = null;
export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler;
}

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLoginCall = error.config?.url?.includes('/api/auth/login');
    if (error.response?.status === 401 && !isLoginCall && onUnauthorized) {
      onUnauthorized();
    }
    return Promise.reject(error);
  }
);

// Turns any error into { status, message, errors } that screens can show
export function getApiError(error) {
  if (error.response?.data) {
    const data = error.response.data;
    return {
      status: error.response.status,
      message: data.message || 'Something went wrong.',
      errors: data.errors || null,
    };
  }
  return {
    status: 0,
    message: 'Cannot reach the server. Check that the API is running.',
    errors: null,
  };
}

export default api;
