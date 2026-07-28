import { NativeModules } from 'react-native';

/**
 * AdMob reklam birim kimlikleri.
 *
 * ÖNEMLİ: react-native-google-mobile-ads native modülü Expo Go'da YOKTUR ve
 * kütüphane import anında native modülü aradığı için Expo Go'da CRASH eder.
 * Bu yüzden kütüphane statik olarak değil, yalnızca native modül varsa
 * (gerçek/dev build) lazy `require` ile yüklenir. Expo Go'da reklamlar atlanır,
 * uygulama normal çalışır.
 */

/** Native reklam modülü bu binary'de var mı? (Expo Go → false, dev build → true) */
export const adsNativeAvailable = !!NativeModules?.RNGoogleMobileAdsModule;

export const IS_DEV = __DEV__;

// Üretim reklam birim ID'leri (kendi AdMob ID'lerinizle değiştirin).
const BANNER_REAL = 'ca-app-pub-XXXXXXXXXXXXXXXX/1111111111';
const INTERSTITIAL_REAL = {
  collect: 'ca-app-pub-XXXXXXXXXXXXXXXX/3333333333',
  export: 'ca-app-pub-XXXXXXXXXXXXXXXX/4444444444',
};

/** Lazy yükleme — yalnızca native modül varken çağrılır. */
function lib(): any {
  return require('react-native-google-mobile-ads');
}

export function bannerUnitId(): string {
  if (!adsNativeAvailable) return '';
  return IS_DEV ? lib().TestIds.BANNER : BANNER_REAL;
}

export function interstitialUnitId(trigger: 'collect' | 'export'): string {
  if (!adsNativeAvailable) return '';
  return IS_DEV ? lib().TestIds.INTERSTITIAL : INTERSTITIAL_REAL[trigger];
}

/** Premium IAP ürün SKU'ları (react-native-iap). */
export const PREMIUM_SKUS = {
  monthly: 'com.dairom.app.premium.monthly',
  yearly: 'com.dairom.app.premium.yearly',
};
