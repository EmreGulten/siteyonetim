import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import { API_BASE_URL } from '../theme/colors';
import { useAuthStore } from '../store/authStore';
import { tokenStorage } from '../utils/secureStore';

/**
 * Merkezî axios istemcisi.
 *  - Her isteğe Authorization: Bearer <token> ekler (interceptor).
 *  - Token'daki SiteId'yi X-Site-Id header'ı olarak ekler (multi-tenancy).
 *  - 401 durumunda bir kez refresh dener; yine başarısızsa otomatik logout.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

// İstek interceptor: token + SiteId
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { token, user } = useAuthStore.getState();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
    // Multi-tenancy: yönetici sadece kendi sitesinin verisini görsün.
    if (user?.siteId) config.headers.set('X-Site-Id', user.siteId);
  }
  return config;
});

// Yanıt interceptor: 401 → refresh veya logout
let refreshing = false;

apiClient.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    if (error.response?.status === 401 && !original._retry && !refreshing) {
      original._retry = true;
      refreshing = true;
      try {
        const refreshToken = await tokenStorage.getRefreshToken();
        const oldToken = await tokenStorage.getToken();
        if (refreshToken && oldToken) {
          const { data } = await axios.post(`${API_BASE_URL}/api/auth/refresh`, {
            accessToken: oldToken,
            refreshToken,
          });
          await useAuthStore.getState().setAuth(data.token.accessToken, data, data.token.refreshToken);
          original.headers.set('Authorization', `Bearer ${data.token.accessToken}`);
          refreshing = false;
          return apiClient(original);
        }
      } catch {
        // refresh başarısız → otomatik logout
      }
      refreshing = false;
      await useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  }
);
