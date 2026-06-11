import { create } from 'zustand';
import type { AuthUserDto } from '@/types/auth';
import { clearTokens, setTokens } from '@/lib/auth';
import { backendService } from '@/services/backend';
import type { LoginFormValues, RegisterFormValues } from '@/lib/validations/auth';

interface AuthState {
  user: AuthUserDto | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setUser: (user: AuthUserDto | null) => void;
  setLoading: (isLoading: boolean) => void;
  login: (values: LoginFormValues) => Promise<AuthUserDto>;
  register: (values: RegisterFormValues) => Promise<AuthUserDto>;
  loadCurrentUser: () => Promise<AuthUserDto | null>;
  logout: () => void;
  logoutRemote: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoading: true,
  isAuthenticated: false,
  setUser: (user) =>
    set({
      user,
      isAuthenticated: !!user,
      isLoading: false,
    }),
  setLoading: (isLoading) => set({ isLoading }),
  login: async (values) => {
    set({ isLoading: true });
    try {
      const response = await backendService.login(values);
      setTokens(response.accessToken, response.refreshToken);
      set({ user: response.user, isAuthenticated: true, isLoading: false });
      return response.user;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },
  register: async (values) => {
    set({ isLoading: true });
    try {
      const response = await backendService.register({
        email: values.email,
        password: values.password,
        fullName: values.fullName,
      });
      setTokens(response.accessToken, response.refreshToken);
      set({ user: response.user, isAuthenticated: true, isLoading: false });
      return response.user;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },
  loadCurrentUser: async () => {
    set({ isLoading: true });
    try {
      const user = await backendService.getCurrentUser();
      set({ user, isAuthenticated: true, isLoading: false });
      return user;
    } catch (error) {
      clearTokens();
      set({ user: null, isAuthenticated: false, isLoading: false });
      throw error;
    }
  },
  logout: () => {
    clearTokens();
    set({ user: null, isAuthenticated: false, isLoading: false });
  },
  logoutRemote: async () => {
    try {
      await backendService.logout();
    } finally {
      clearTokens();
      set({ user: null, isAuthenticated: false, isLoading: false });
    }
  },
}));
