import { create } from 'zustand';
import { tokenStorage } from '../utils/secureStore';

export interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
  role: 'SuperAdmin' | 'SiteManager' | 'Resident';
  siteId: string | null;
  isPremium: boolean;
  premiumExpiryDate?: string | null;
}

type AuthModalMode = 'login' | 'register';

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  hydrated: boolean;
  // Misafir modunda write aksiyonlarında açılan auth modalı
  authModalVisible: boolean;
  authModalMode: AuthModalMode;
  setAuth: (token: string, user: AuthUser, refreshToken: string) => Promise<void>;
  updateUser: (patch: Partial<AuthUser>) => Promise<void>;
  hydrate: () => Promise<void>;
  logout: () => Promise<void>;
  openAuthModal: (mode?: AuthModalMode) => void;
  closeAuthModal: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  token: null,
  refreshToken: null,
  user: null,
  hydrated: false,
  authModalVisible: false,
  authModalMode: 'login',

  setAuth: async (token, user, refreshToken) => {
    await tokenStorage.save(token, refreshToken, user);
    set({ token, user, refreshToken, authModalVisible: false });
  },

  updateUser: async (patch) => {
    const { user, token, refreshToken } = get();
    if (!user || !token || !refreshToken) return;
    const next = { ...user, ...patch };
    await tokenStorage.save(token, refreshToken, next);
    set({ user: next });
  },

  hydrate: async () => {
    const token = await tokenStorage.getToken();
    const refreshToken = await tokenStorage.getRefreshToken();
    const user = await tokenStorage.getUser<AuthUser>();
    set({ token, user, refreshToken, hydrated: true });
  },

  logout: async () => {
    await tokenStorage.clear();
    set({ token: null, refreshToken: null, user: null });
  },

  openAuthModal: (mode = 'login') => set({ authModalVisible: true, authModalMode: mode }),
  closeAuthModal: () => set({ authModalVisible: false }),
}));
