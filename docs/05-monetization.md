# FAZ 5 — Ticarileştirme (AdMob + Premium IAP)

## 5.1 — Özellik Haritası (Free vs Premium)

| Özellik | Ücretsiz | Premium |
|--------|----------|---------|
| Site sayısı | 1 | Sınırsız |
| Daire sayısı | **en fazla 20** (`PremiumPolicy.FreeMaxApartments`) | Sınırsız |
| Aidat & Gider | ✓ | ✓ |
| Ek Aidat yönetimi | ✗ | ✓ |
| Raporlarda reklam | ✓ (Banner) | Reklamsız |
| Makbuz | "Ücretsiz Plan ile Oluşturuldu" watermark | Özel branded |
| KMK detaylı rapor | ✗ | ✓ |
| Destek | Topluluk | Öncelikli |

**Uygulama noktaları:**
- `PremiumPolicy` (backend) — `EnsureCanAddApartmentAsync` (20 sınır), `EnsureCanManageExtraDuesAsync`.
- `ReceiptService` — makbuza `IsFreePlan` watermark (QuestPDF).
- `BannerAdSlot` / `useInterstitialAd` (mobil) — premium ise no-op/null.

## 5.2 — AdMob (react-native-google-mobile-ads)

| Tip | Yer | Tetik |
|-----|-----|-------|
| **Banner** | Dashboard + Raporlar en alt | Sabit |
| **Interstitial** | Hızlı Tahsilat ("Makbuz Oluştur"), Raporlar ("Excel İndir") | Buton öncesi → reklam kapandıktan sonra işlem |

`useInterstitialAd(trigger).showThen(action)` — reklam yüklenmişse gösterir, kapandığında `action`'ı çalıştırır. Premium'da atlanır. `app.json`'a config plugin eklendi (androidAppId/iosAppId). Test ID'leri `__DEV__`'de otomatik.

## 5.3 — Güvenli Premium Satın Alma

Sadece mobilde kontrol **hacklenebilir**. Güvenli mimari:

```
[Mobil] react-native-iap → store'dan receipt (purchaseToken/transactionReceipt)
   │
   ▼ POST /api/subscription/verify
[Backend] StoreReceiptVerifier → Google Play Developer API / Apple App Store Server API
   │   - paymentState kontrolü (gerçekten ödendi mi?)
   │   - sahte/dolandırıcılık tespiti
   ▼
[DB] Users: Plan = Premium, PremiumExpiryDate güncellenir
   │
   ▼ GET /api/subscription/status (uygulama açılışında)
[Mobil] IsPremium'e göre reklamlar/butonlar/UI güncellenir
```

**Backend parçaları (FAZ 3-5):**
- `StoreReceiptVerifier` — Google `purchases.subscriptions.get` + Apple `transactions`.
- `GoogleJwtAccessTokenProvider` — RS256 service-account JWT → OAuth access token (önbellekli).
- `AppleJwtProvider` — ES256 imzalı App Store Server API JWT.
- `SubscriptionService` — doğrulama sonucu User güncelleme.

**Mobil parçalar:**
- `usePurchases` — `requestPurchase` → `purchaseUpdatedListener` → backend verify.
- `PremiumScreen` — fiyatları listele, satın al.
- `config/ads.ts` — birim ID'leri + IAP SKU'ları.

> Not: Doğrulama anahtarları (Google SA JSON, Apple P8) `deploy/.env` + `secrets/` üzerinden
> volume olarak container'a bağlanır (`GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_PATH`, `APPLE_*`).
