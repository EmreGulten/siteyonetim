# FAZ 4 — Frontend (React Native / Expo)

## Kütüphaneler (Adım 4.1)
| Kütüphane | Kullanım |
|-----------|----------|
| `axios` | HTTP istemcisi + interceptor |
| `@tanstack/react-query` | Veri önbellekleme, loading/error state, senkronizasyon |
| `zustand` | UI/auth state (token, kullanıcı) |
| `expo-secure-store` | JWT güvenli saklama (Keychain/Keystore) |
| `react-native-chart-kit` | Dairesel ve çizgi grafikler |
| `expo-barcode-scanner` / `expo-file-system` / `expo-sharing` | Barkod, dosya indir/paylaş |

## Auth Flow & Multi-Tenancy (Adım 4.2)
- `LoginScreen` → API login → `authStore.setAuth` → JWT **SecureStore**'a yazılır.
- `api/client.ts` **request interceptor**: her isteğe `Authorization: Bearer <token>` + `X-Site-Id` ekler.
- **response interceptor**: `401` → bir kez refresh dener, yine başarısızsa **otomatik logout**.
- Uygulama açılışında `hydrate()` token/user'ı SecureStore'tan yükler.

## Ekranlar (Adım 4.3)
| Ekran | İçerik |
|-------|--------|
| **Dashboard** | Bakiye kartları (Tahsil/Borç/Gider/Net), dairesel grafik (Tahsilat Oranı), son 5 işlem, banner |
| **Daireler** | Blok bazlı filtreleme (Chip), liste → detay |
| **Daire Detayı** | Aylık aidat çizgi grafiği (aidat vs ödenen) |
| **Hızlı Tahsilat** | Aidat bul → tutar gir → "Ödendi" → PDF makbuz indir |
| **Raporlar** | Excel/CSV indir + progress bar; KMK (Premium); banner |

## Kurulum (native modüller gerektirir — dev build)
```bash
cd mobile
npm install
npx expo prebuild          # native modüller (chart-kit, ads, iap) için
npx expo run:android       # veya run:ios
```
`EXPO_PUBLIC_API_BASE_URL` ile API adresini override edin.
