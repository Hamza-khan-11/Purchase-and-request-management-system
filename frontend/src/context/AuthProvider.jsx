import { useCallback, useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { AuthContext } from './authContext';
import { setUnauthorizedHandler, tokenStore } from '../services/api';
import * as authService from '../services/authService';

const USER_KEY = 'pa_user';
const EXPIRES_KEY = 'pa_expires';

function readStoredUser() {
  const token = tokenStore.get();
  const expires = localStorage.getItem(EXPIRES_KEY);
  if (!token || !expires || new Date(expires) <= new Date()) return null;
  try {
    return JSON.parse(localStorage.getItem(USER_KEY));
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser);

  const clearSession = useCallback(() => {
    tokenStore.clear();
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    setUser(null);
  }, []);

  // Any 401 from the API logs the user out
  useEffect(() => {
    setUnauthorizedHandler(() => {
      if (tokenStore.get()) {
        clearSession();
        toast.error('Your session has expired. Please log in again.');
      }
    });
  }, [clearSession]);

  const login = useCallback(async (email, password) => {
    const data = await authService.login(email, password);
    tokenStore.set(data.token);
    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
    localStorage.setItem(EXPIRES_KEY, data.expiresAt);
    setUser(data.user);
    return data.user;
  }, []);

  const logout = useCallback(async () => {
    try {
      await authService.logout();
    } catch {
      // Even if the server call fails we still log out locally
    }
    clearSession();
  }, [clearSession]);

  const value = useMemo(() => ({ user, login, logout }), [user, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
