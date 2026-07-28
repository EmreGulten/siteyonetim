import 'react-native-gesture-handler';
import React, { useEffect, useRef } from 'react';
import { StatusBar } from 'react-native';
import { Provider as PaperProvider, DefaultTheme } from 'react-native-paper';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import Purchases from 'react-native-purchases';
import Constants from 'expo-constants';
import { useAuthStore } from './src/store/authStore';
import { AppNavigator } from './src/navigation/AppNavigator';
import { colors } from './src/theme/colors';

// RevenueCat public SDK key (Apple App Store). Public — uygulamaya gömülür.
const RC_IOS_KEY = process.env.EXPO_PUBLIC_REVENUECAT_IOS_KEY;

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000, refetchOnWindowFocus: false } },
});

const theme = {
  ...DefaultTheme,
  colors: { ...DefaultTheme.colors, primary: colors.primary, accent: colors.accent },
};

export default function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  const hydrated = useAuthStore((s) => s.hydrated);
  const userId = useAuthStore((s) => s.user?.userId);
  const configured = useRef(false);

  useEffect(() => {
    // Uygulama açılışında token/user'ı SecureStore'tan yükle.
    hydrate();
  }, [hydrate]);

  // RevenueCat'i bir kez başlat. Expo Go'da RNPurchases native module olmadığından
  // configure hata fırlatır → sessizce atla (Premium bu derlemede devre dışı kalır;
  // dev build / production'da native module mevcuttur).
  useEffect(() => {
    if (configured.current || !RC_IOS_KEY) return;
    // Expo Go'da native store yok → configure Red Box açar; tamamen atla.
    // (dev/production build'te appOwnership !== 'expo' → normal configure edilir)
    if (Constants.appOwnership === 'expo') return;
    try {
      Purchases.configure({ apiKey: RC_IOS_KEY });
      configured.current = true;
    } catch {
      configured.current = false;
    }
  }, []);

  // RevenueCat appUserID = backend User.Id. Girişte logIn, çıkışta logOut.
  // Böylece backend /verify ile subscribers/{userId} sorgusu tutarlı çalışır.
  useEffect(() => {
    if (!configured.current) return;
    try {
      if (userId) {
        Purchases.logIn(userId).catch(() => { /* zaten bağlı olabilir */ });
      } else {
        Purchases.logOut().catch(() => {});
      }
    } catch {
      /* native module yok (Expo Go) */
    }
  }, [userId]);

  if (!hydrated) return null; // splash

  return (
    <SafeAreaProvider>
      <QueryClientProvider client={queryClient}>
        <PaperProvider theme={theme}>
          <StatusBar barStyle="light-content" backgroundColor={colors.primary} />
          <AppNavigator />
        </PaperProvider>
      </QueryClientProvider>
    </SafeAreaProvider>
  );
}
