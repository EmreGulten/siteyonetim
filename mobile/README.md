# Site Yönetimi — Mobile (React Native / Expo)

Mobil istemci. JWT → SecureStore, react-query cache, zustand state, chart-kit grafikler.

## Yapı
```
App.tsx                      # Provider'lar (Query, Paper, SafeArea) + hydrate
src/
├── api/client.ts            # axios + interceptor (Bearer, X-Site-Id, 401→logout)
├── api/endpoints.ts         # tüm API çağrıları
├── store/authStore.ts       # zustand: token, user (SiteId, Role, IsPremium)
├── utils/secureStore.ts     # expo-secure-store wrapper
├── utils/download.ts        # PDF/CSV indir + paylaş (+progress)
├── navigation/AppNavigator.tsx
├── screens/                 # Login, Dashboard, Apartments, Detail, QuickCollection, Reports
└── components/              # BalanceCard, ProgressBar, BannerAdSlot
```

## Notlar
- `react-native-chart-kit`, `react-native-google-mobile-ads`, `react-native-iap` native
  modüllerdir → `expo prebuild` + dev build gerektirir (Expo Go yetersiz).
- FAZ 5'te `useAds` ve `BannerAdSlot` gerçek AdMob ile, `react-native-iap` satın alma ile değiştirilecek.
