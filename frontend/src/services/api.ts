import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponse } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7043/api';
let accessToken: string | null = null;
let onSessionExpired: (() => void) | null = null;
let refreshing: Promise<string | null> | null = null;

export const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true
});

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}

export function setSessionExpiredHandler(handler: (() => void) | null) {
  onSessionExpired = handler;
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  const csrfToken = readCookie('XSRF-TOKEN');
  if (csrfToken && ['post', 'put', 'patch', 'delete'].includes((config.method ?? 'get').toLowerCase())) {
    config.headers['X-CSRF-TOKEN'] = csrfToken;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
    const status = error.response?.status;
    const url = original?.url ?? '';

    if (status !== 401 || !original || original._retry || url.includes('/auth/login') || url.includes('/auth/verify-2fa') || url.includes('/auth/refresh')) {
      if (status === 401 && url.includes('/auth/refresh')) {
        setAccessToken(null);
        onSessionExpired?.();
      }

      throw error;
    }

    original._retry = true;
    const newToken = await refreshAccessToken();
    if (!newToken) {
      setAccessToken(null);
      onSessionExpired?.();
      throw error;
    }

    original.headers.Authorization = `Bearer ${newToken}`;
    return api(original);
  }
);

export async function refreshAccessToken() {
  refreshing ??= api
    .post<RefreshTokenResponse>('/auth/refresh')
    .then((response) => {
      setAccessToken(response.data.accessToken);
      return response.data.accessToken;
    })
    .catch(() => null)
    .finally(() => {
      refreshing = null;
    });

  return refreshing;
}

function readCookie(name: string) {
  const match = document.cookie
    .split('; ')
    .find((row) => row.startsWith(`${encodeURIComponent(name)}=`));

  return match ? decodeURIComponent(match.split('=')[1]) : null;
}

export function getProblemMessage(error: unknown) {
  if (error instanceof Error && !axios.isAxiosError(error)) {
    return error.message;
  }

  if (axios.isAxiosError(error)) {
    const status = error.response?.status;
    const detail = (error.response?.data as { detail?: string; message?: string } | undefined)?.detail ??
      (error.response?.data as { message?: string } | undefined)?.message;

    if (detail) return detail;
    if (status === 401) return 'La sesion ha expirado.';
    if (status === 403) return 'No tiene permisos para realizar esta accion.';
    if (status === 409) return 'El recurso fue modificado por otro usuario.';
    if (status === 413) return 'El archivo supera el tamano maximo permitido.';
    if (status === 415) return 'El tipo de archivo no esta permitido.';
    if (status === 429) return 'Demasiados intentos. Intente nuevamente en unos minutos.';
  }

  return 'No se pudo completar la operacion.';
}
