import * as SecureStore from 'expo-secure-store';

const KEY_TOKEN = 'auth_token';        // access token
const KEY_REFRESH = 'auth_refresh';    // refresh token
const KEY_USER = 'auth_user';          // serileştirilmiş kullanıcı (SiteId, Role, IsPremium)

export const tokenStorage = {
  async save(token: string, refreshToken: string, user: unknown) {
    await SecureStore.setItemAsync(KEY_TOKEN, token);
    await SecureStore.setItemAsync(KEY_REFRESH, refreshToken);
    await SecureStore.setItemAsync(KEY_USER, JSON.stringify(user));
  },
  async getToken() {
    return (await SecureStore.getItemAsync(KEY_TOKEN)) ?? null;
  },
  async getRefreshToken() {
    return (await SecureStore.getItemAsync(KEY_REFRESH)) ?? null;
  },
  async getUser<T>(): Promise<T | null> {
    const raw = await SecureStore.getItemAsync(KEY_USER);
    return raw ? (JSON.parse(raw) as T) : null;
  },
  async clear() {
    await SecureStore.deleteItemAsync(KEY_TOKEN);
    await SecureStore.deleteItemAsync(KEY_REFRESH);
    await SecureStore.deleteItemAsync(KEY_USER);
  },
};
