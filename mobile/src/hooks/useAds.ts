import { useEffect, useRef, useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { adsNativeAvailable, interstitialUnitId } from '../config/ads';

// SDK tek seferlik başlatma
let initialized = false;
async function ensureInit() {
  if (!adsNativeAvailable || initialized) return;
  initialized = true;
  try {
    const ads: any = require('react-native-google-mobile-ads');
    await ads.mobileAds().setMaxAdContentRating(ads.MaxAdContentRating.PG);
    await ads.mobileAds().initialize();
  } catch {
    /* yut */
  }
}

/**
 * Interstitial reklam. Premium kullanıcılar için no-op.
 * Native reklam modülü yoksa (Expo Go) da no-op — uygulama crash olmaz.
 * showThen(action): reklam yüklüyse gösterilir, kapatılınca action çalışır;
 * yüklenmediyse/premium/yoksa reklam atlanır.
 */
export function useInterstitialAd(trigger: 'collect' | 'export') {
  const premium = useAuthStore((s) => s.user?.isPremium);
  const adRef = useRef<any>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (premium || !adsNativeAvailable) return; // premium = reklamsız; modül yoksa atla
    ensureInit();
    const ads: any = require('react-native-google-mobile-ads');
    const ad = ads.InterstitialAd.createForAdRequest(interstitialUnitId(trigger), {
      requestNonPersonalizedAdsOnly: false,
    });
    const u1 = ad.addAdEventListener(ads.AdEventType.LOADED, () => setReady(true));
    const u2 = ad.addAdEventListener(ads.AdEventType.ERROR, () => setReady(false));
    const u3 = ad.addAdEventListener(ads.AdEventType.CLOSED, () => { setReady(false); ad.load(); });
    ad.load();
    adRef.current = ad;
    return () => { u1(); u2(); u3(); };
  }, [premium, trigger]);

  return {
    ready: premium || !adsNativeAvailable ? false : ready,
    showThen(action: () => void) {
      if (premium || !adsNativeAvailable || !adRef.current) { action(); return; }
      try {
        const ads: any = require('react-native-google-mobile-ads');
        const ad = adRef.current;
        const unsub = ad.addAdEventListener(ads.AdEventType.CLOSED, () => { unsub(); action(); });
        ad.show();
      } catch {
        action(); // reklam gösterilemezse işlemi engelleme
      }
    },
  };
}
