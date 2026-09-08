import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from 'react';
import { api, setAccessToken, setSessionExpiredHandler } from '../../services/api';
import type { AuthenticatedUser, LoginResponse, RefreshTokenResponse } from '../../types';

interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  login: (usernameOrEmail: string, password: string) => Promise<LoginResponse>;
  verifyTwoFactor: (twoFactorToken: string, code: string) => Promise<LoginResponse>;
  refreshSession: () => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: AuthenticatedUser | null) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);

  useEffect(() => {
    setSessionExpiredHandler(() => {
      setUser(null);
      setAccessToken(null);
    });

    return () => setSessionExpiredHandler(null);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: Boolean(user),
    async login(usernameOrEmail, password) {
      const response = await api.post<LoginResponse>('/auth/login', { usernameOrEmail, password });
      if (response.data.accessToken && response.data.user) {
        setAccessToken(response.data.accessToken);
        setUser(response.data.user);
      }

      return response.data;
    },
    async verifyTwoFactor(twoFactorToken, code) {
      const response = await api.post<LoginResponse>('/auth/verify-2fa', { twoFactorToken, code });
      if (response.data.accessToken && response.data.user) {
        setAccessToken(response.data.accessToken);
        setUser(response.data.user);
      }

      return response.data;
    },
    async refreshSession() {
      const response = await api.post<RefreshTokenResponse>('/auth/refresh');
      setAccessToken(response.data.accessToken);
      setUser(response.data.user);
    },
    async logout() {
      try {
        await api.post('/auth/logout');
      } finally {
        setAccessToken(null);
        setUser(null);
      }
    },
    setUser
  }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
