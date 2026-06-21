import { create } from 'zustand';
import type { AuthUserDto } from '@/types/auth';
import { clearTokens, setTokens } from '@/lib/auth';
import { getApiErrorMessage, getApiStatus } from '@/lib/api';
import { backendService } from '@/services/backend';
import type { LoginFormValues, RegisterFormValues } from '@/lib/validations/auth';

interface AuthState {
  user: AuthUserDto | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  sessionError: string | null;
  setUser: (user: AuthUserDto | null) => void;
  setLoading: (isLoading: boolean) => void;
  login: (values: LoginFormValues) => Promise<AuthUserDto>;
  register: (values: RegisterFormValues) => Promise<AuthUserDto>;
  loadCurrentUser: (options?: { allowAnonymous?: boolean }) => Promise<AuthUserDto | null>;
  logout: () => void;
  logoutRemote: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoading: true,
  isAuthenticated: false,
  sessionError: null,
  setUser: (user) =>
    set({
      user,
      isAuthenticated: !!user,
      isLoading: false,
      sessionError: null,
    }),
  setLoading: (isLoading) => set({ isLoading }),
  login: async (values) => {
    set({ isLoading: true, sessionError: null });
    try {
      const response = await backendService.login(values);
      setTokens(response.accessToken, response.refreshToken);
      set({ user: response.user, isAuthenticated: true, isLoading: false, sessionError: null });
      return response.user;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },
  register: async (values) => {
    set({ isLoading: true, sessionError: null });
    try {
      const response = await backendService.register({
        email: values.email,
        password: values.password,
        fullName: values.fullName,
      });
      setTokens(response.accessToken, response.refreshToken);
      set({ user: response.user, isAuthenticated: true, isLoading: false, sessionError: null });
      return response.user;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },
  loadCurrentUser: async (options) => {
    set({ isLoading: true, sessionError: null });
    try {
      const user = await backendService.getCurrentUser(options);
      set({ user, isAuthenticated: true, isLoading: false, sessionError: null });
      return user;
    } catch (error) {
      if (getApiStatus(error) === 401) {
        clearTokens();
        set({ user: null, isAuthenticated: false, isLoading: false, sessionError: null });
      } else {
        // Network, rate-limit, and server errors must not invalidate a valid session.
        set({ isLoading: false, sessionError: getApiErrorMessage(error) });
      }
      throw error;
    }
  },
  logout: () => {
    clearTokens();
    set({ user: null, isAuthenticated: false, isLoading: false, sessionError: null });
  },
  logoutRemote: async () => {
    try {
      await backendService.logout();
    } finally {
      clearTokens();
      set({ user: null, isAuthenticated: false, isLoading: false, sessionError: null });
    }
  },
}));
