import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert } from 'react-native';
import Purchases, { PURCHASES_ERROR_CODE } from 'react-native-purchases';
import { subscriptionApi } from '../api/endpoints';
import { PREMIUM_SKUS } from '../config/ads';
import { useAuthStore } from '../store/authStore';

/**
 * Premium abonelik hook'u — RevenueCat (react-native-purchases) üzerinden.
 * Akış:
 *   1) buy() → StoreKit satın alma (RevenueCat yönetir).
 *   2) customerInfo.entitlements.active["premium"] → aktif mi?
 *   3) aktifse backend /api/subscription/verify (appUserId = User.Id) →
 *      backend RevenueCat REST ile teyit eder → User.IsPremium güncellenir.
 * Premium durumu sunucuda zorlanır; mobil tek başına hacklense bile geçersiz.
 * Apple abonelikler için "Restore" şarttır → restore().
 */
export interface PremiumProduct {
  productId: string;
  title: string;
  price: string;
  period: string;
  highlight?: boolean;
}

const ENTITLEMENT = 'premium';

const PRODUCTS: PremiumProduct[] = [
  { productId: PREMIUM_SKUS.monthly, title: 'Aylık Premium', price: '₺49,99', period: '/ay' },
  { productId: PREMIUM_SKUS.yearly, title: 'Yıllık Premium', price: '₺399,99', period: '/yıl', highlight: true },
];

export function usePurchases() {
  const qc = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const updateUser = useAuthStore((s) => s.updateUser);
  const userId = user?.userId;

  const status = useQuery({
    queryKey: ['subscription-status'],
    queryFn: () => subscriptionApi.status(),
  });

  // Satın alma/restore sonrası backend'i RevenueCat durumuyla senkronla.
  const syncBackend = async () => {
    if (!userId) return;
    try {
      // RevenueCat appUserId = backend User.Id. Backend entitlement'ı sunucuda doğrular.
      const res = await subscriptionApi.verify('Apple', userId, PREMIUM_SKUS.monthly);
      await updateUser({
        isPremium: !!res.isPremium,
        premiumExpiryDate: res.premiumExpiryDate,
      });
    } catch {
      // Sessiz: UI, customerInfo'dan zaten güncellenir; backend isteği başarısız olursa
      // bir sonraki açılışta /status tekrar dengeleyecek.
    }
    await qc.invalidateQueries({ queryKey: ['subscription-status'] });
  };

  const buy = async (productId: string) => {
    try {
      const products = await Purchases.getProducts([productId]);
      if (products.length === 0) {
        Alert.alert('Ürün bulunamadı', 'Premium ürünü App Store’dan alınamadı. App Store Connect / RevenueCat eşleşmesini kontrol edin.');
        return;
      }
      const { customerInfo } = await Purchases.purchaseStoreProduct(products[0]);
      const active = !!customerInfo.entitlements.active[ENTITLEMENT];
      if (active) {
        await syncBackend();
        Alert.alert('Premium aktif 🎉', 'Premium özellikler kullanıma açıldı.');
      } else {
        Alert.alert('Tamamlanamadı', 'Satın alma işlendi ama entitlement aktif değil. Geri yüklemeyi deneyin.');
      }
    } catch (e: unknown) {
      const code = (e as { code?: unknown })?.code;
      if (code === PURCHASES_ERROR_CODE.PURCHASE_CANCELLED_ERROR) return; // kullanıcı iptal etti
      Alert.alert('Hata', 'Satın alma başarısız. Tekrar deneyin.');
    }
  };

  const restore = async () => {
    try {
      const customerInfo = await Purchases.restorePurchases();
      const active = !!customerInfo.entitlements.active[ENTITLEMENT];
      await syncBackend();
      Alert.alert(
        active ? 'Geri yüklendi' : 'Kayıt yok',
        active ? 'Premium aboneliğin geri yüklendi.' : 'Bu Apple kimliğinde aktif satın alma bulunamadı.',
      );
    } catch {
      Alert.alert('Hata', 'Geri yükleme başarısız.');
    }
  };

  return {
    products: PRODUCTS,
    buy,
    restore,
    loading: status.isFetching,
    error: status.error ? 'Durum alınamadı.' : null,
    status: status.data,
    isPremium: !!user?.isPremium,
  };
}
