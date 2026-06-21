import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { getAccessToken, getRefreshToken, setTokens, clearTokens } from './auth';
import type { ApiResponse, ErrorResponse } from '@/types/api';
import type { AuthResponse } from '@/types/auth';
import { API_BASE_URL } from './config';

declare module 'axios' {
  export interface AxiosRequestConfig {
    skipForbiddenRedirect?: boolean;
    allowAnonymousFallback?: boolean;
  }

  export interface InternalAxiosRequestConfig {
    skipForbiddenRedirect?: boolean;
    allowAnonymousFallback?: boolean;
  }
}

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

function isPublicAuthRequest(url?: string) {
  return Boolean(url && [
    '/auth/login',
    '/auth/register',
    '/auth/forgot-password',
    '/auth/reset-password',
    '/auth/verify-email',
    '/auth/refresh-token',
  ].some((path) => url.includes(path)));
}

// Track if we're currently refreshing the token
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

function processQueue(error: Error | null, token: string | null = null) {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
}

// Request interceptor: attach access token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken();
    if (token && !isPublicAuthRequest(config.url)) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: handle 401 with token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ErrorResponse>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean; _anonymousRetry?: boolean };

    if (error.response?.status === 403 && !originalRequest.skipForbiddenRedirect && !isPublicAuthRequest(originalRequest.url)) {
      if (typeof window !== 'undefined' && window.location.pathname !== '/forbidden') {
        window.location.href = '/forbidden';
      }
      return Promise.reject(error);
    }

    // If 401 and we haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      // A failed login belongs to the form; redirecting here would erase its error.
      if (originalRequest.url?.includes('/auth/login')) {
        return Promise.reject(error);
      }

      if (originalRequest.url?.includes('/auth/refresh-token')) {
        clearTokens();
        if (typeof window !== 'undefined') {
          window.location.href = '/auth/login';
        }
        return Promise.reject(error);
      }

      if (isRefreshing) {
        // Queue this request until refresh completes
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = getRefreshToken();
      if (!refreshToken) {
        if (originalRequest.allowAnonymousFallback) return retryAnonymously(originalRequest);
        clearSessionAndRedirect();
        return Promise.reject(error);
      }

      try {
        const { data } = await axios.post<ApiResponse<AuthResponse>>(
          `${API_BASE_URL}/auth/refresh-token`,
          { refreshToken }
        );

        const newAccessToken = data.data.accessToken;
        const newRefreshToken = data.data.refreshToken;
        setTokens(newAccessToken, newRefreshToken);

        processQueue(null, newAccessToken);

        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError as Error);
        if (originalRequest.allowAnonymousFallback) return retryAnonymously(originalRequest);
        clearSessionAndRedirect();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

function clearSessionAndRedirect() {
  clearTokens();
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event('startupconnect:session-cleared'));
    window.location.href = '/auth/login';
  }
}

function retryAnonymously(config: InternalAxiosRequestConfig & { _retry?: boolean; _anonymousRetry?: boolean }) {
  if (config._anonymousRetry) return Promise.reject(new Error('Anonymous fallback failed'));
  config._anonymousRetry = true;
  config._retry = true;
  clearTokens();
  delete config.headers.Authorization;
  if (typeof window !== 'undefined') window.dispatchEvent(new Event('startupconnect:session-cleared'));
  return api(config);
}

export default api;

export function getApiStatus(error: unknown): number | undefined {
  return axios.isAxiosError(error) ? error.response?.status : undefined;
}

/**
 * Extract error message from API error response
 */
export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ErrorResponse | undefined;
    if (data?.message) return data.message;
    if (data?.errors?.length) return data.errors.map((e) => e.message).join(', ');
    if (error.message) return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred';
}

/**
 * Extract field-level errors from API error response
 */
export function getApiFieldErrors(error: unknown): Record<string, string> {
  const fieldErrors: Record<string, string> = {};
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ErrorResponse | undefined;
    if (data?.errors) {
      for (const err of data.errors) {
        if (err.field) {
          fieldErrors[err.field] = err.message;
        }
      }
    }
  }
  return fieldErrors;
}
