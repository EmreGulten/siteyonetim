# FAZ 3 — Backend (.NET Core 8 Web API)

## Clean Architecture Katmanları

```
Api          → Controllers, Middleware, Program.cs (host: JWT, CORS, rate limit, Hangfire, Swagger)
  ↓
Application  → Servisler (iş mantığı) + DTO'lar + Abstractions (arayüzler)
  ↓
Domain       → Entity'ler, enumlar, value object (EF bağımsız)
Infrastructure → DbContext, FluentAPI, JWT, MinIO, QuestPDF, Hangfire, Store IAP
  ↑ (Domain'i ve Application'ı uygular)
```

Bağımlılık yönü: `Api → Application/Infrastructure → Domain`. Application, Infrastructure'a değil tersine bağlıdır (abstraction'lar Application'da: `IAppDbContext`, `ICurrentUserService`, `IFileStorage`, `ITokenService`, `IReceiptPdfRenderer`, `IStoreReceiptVerifier`).

## Servisler (Application)

| Servis | Görev |
|--------|-------|
| `AuthService` | Kayıt/giriş, JWT + refresh token, brute-force kilidi |
| `DuesGenerationService` | Ayın 1'inde aidat üretir (tip → muafiyet → ek aidat → Breakdown JSONB) |
| `CollectionService` | Hızlı tahsilat (kısmi ödeme), durum güncelleme, gelir kaydı, makbuz |
| `FinancialSummaryService` | Dashboard: SQL aggregate (Sum) ile beklenen/tahsil/gider/net |
| `ReportService` | Borçlu daireler (TC maskeli), KMK raporu |
| `ReceiptService` | Dues → ReceiptData → QuestPDF, MinIO'ya yükle |
| `ApartmentService` | Daire/blok/tip CRUD, aylık aidat grafiği |
| `TransactionService` | Gelir/gider ekle (faturayı MinIO'ya) |
| `SubscriptionService` | IAP receipt güvenli doğrulama (FAZ 5) |

## Güvenlik / Cross-cutting

- **JWT**: token içine `sub`, `email`, `role`, `site_id` claim'leri gömülü.
- **Role-based auth**: `[Authorize(Policy="SiteManager")]` — yalnız yöneticiler veri girer.
- **Multi-tenancy**: `HttpCurrentUserService` (claim'lerden) → `AppDbContext` global query filter.
- **Global exception handler** → tutarlı `ProblemDetails` JSON.
- **Rate limiting**: IP başına 60 istek/dk (token bucket).
- **Hangfire dashboard**: `/hangfire` (yalnız SuperAdmin).

## Endpoint Özeti

```
POST   /api/auth/register | login | refresh
GET    /api/dues                          POST /api/dues/collect | generate
GET    /api/apartments[?blockId]          POST /api/apartments | blocks | types
GET    /api/apartments/{id}/chart
GET    /api/finance/summary/{y}/{m}       GET/POST /api/finance/transactions
GET    /api/reports/debtors | kmk | debtors/export
GET    /api/receipts/generate/{duesId}    → PDF byte[]
POST   /api/subscription/verify           GET /api/subscription/status
GET    /health                            GET /hangfire
```

## Migration

`dotnet ef migrations add InitialCreate` ile üretildi (11 tablo). Program.cs başlangıçta
`db.Database.Migrate()` + `MinioStorageService.EnsureBucketAsync()` çalıştırır.
