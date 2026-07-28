import { useCallback } from 'react';
import { useAuthStore } from '../store/authStore';

/**
 * Write aksiyonları (blok/daire/aidat/gelir/gider ekleme) için auth gate.
 * Misafir (token yok) çağırırsa üye olma modalını açar ve false döner;
 * çağıran handler bu durumda işlemi durdurur (backend'e istek gitmez).
 *
 * Kullanım:
 *   const requireAuth = useRequireAuth();
 *   const onSave = () => {
 *     if (!requireAuth()) return;   // misafirse modal açıldı, çık
 *     // ... write işlemi ...
 *   };
 */
export function useRequireAuth() {
  const token = useAuthStore((s) => s.token);
  const openAuthModal = useAuthStore((s) => s.openAuthModal);

  return useCallback(() => {
    if (token) return true;
    openAuthModal('register');
    return false;
  }, [token, openAuthModal]);
}
