# FAZ 2 — Veritabanı Tasarımı (PostgreSQL / EF Core Code-First)

> Multi-tenant, PostgreSQL'e özgü JSONB & Array avantajlarından yararlanan veri modeli.

---

## 2.1 — Çok Kiracılı (Multi-Tenancy) Stratejisi

**Model:** Tek veritabanı + paylaşılan şema + satır seviyesi izolasyon (`SiteId`).

```
                 ┌─────────────┐
                 │    Site     │  ← kiracı (tenant)
                 │  (anchor)   │
                 └──────┬──────┘
                        │ SiteId FK
        ┌───────────────┼───────────────────────────────┐
        │               │               │               │
   ┌────▼────┐    ┌─────▼─────┐   ┌─────▼─────┐   ┌──────▼──────┐
   │  User   │    │  Block    │   │ExtraDues  │   │Transaction │
   └────┬────┘    └─────┬─────┘   └─────┬─────┘   └─────────────┘
        │ SiteId?        │ SiteId       │ SiteId
        │                ▼              ▼
        │           ┌──────────┐   ┌──────────────────┐
        │           │Apartment │   │ExtraDuesDifference│
        │           │  Type    │   └──────────────────┘
        │           └────┬─────┘
        │                │
   ┌────▼────┐      ┌────▼────┐
   │ Resident│◄─────│ Apartment│
   └─────────┘      └────┬────┘
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
          ┌──────┐  ┌────────┐  ┌───────────┐
          │ Dues │  │Exempt. │  │(Receipt)  │
          └──────┘  └────────┘  └───────────┘
```

### İzolasyon mekanizması (`AppDbContext.ApplyQueryFilters`)

- `TenantEntity` türevi **tüm** tablolara `SiteId` FK eklendi (denormalize — sorgu filtresi doğrudan çalışsın).
- Her `DbSet` sorgusuna global `HasQueryFilter` uygulanır:
  - `SuperAdmin` → tüm siteler (`true` kısa devre).
  - `SiteManager` / `Resident` → yalnız `SiteId == mevcut_kullanıcı.SiteId`.
  - SiteId çözülemezse → boş küme (**fail-safe güvenlik**).
- Soft-delete (`IsDeleted`) da aynı filtrede birleşik uygulanır.
- `Site` ve `User` için ek özel filtre (PK / nullable SiteId).

---

## 2.2 — PostgreSQL'e Özgü Özellikler

| Özellik | Nerede | Nasıl | Fayda |
|--------|--------|-------|-------|
| **JSONB** | `Site.Settings` | `OwnsOne(...).ToJson("settings")` | Şemasız ayar; yeni alan = migration yok; `->>` ile sorgulanır |
| **JSONB** | `Dues.Breakdown` | `.HasColumnType("jsonb")` | Aidat dökümü (base/extra/exemption) esnek saklanır |
| **Array `text[]`** | `Site.Tags` | `.HasConversion(...).HasColumnType("text[]")` | Çoklu etiket; `@>` (contains) sorgusu |
| **GIN Index** | `Site.Tags` | `.HasIndex(...).HasMethod("gin")` | Array contains sorgularını hızlandırır |
| **`xid` / xmin** | tüm tablolar | shadow property `xmin` + `IsConcurrencyToken` | İyimser eşzamanlılık (optimistic concurrency) |
| **`numeric(18,2)`** | tüm para alanları | `HavePrecision(18,2)` | Doğru para hassasiyeti |
| **Benzersiz indeksler** | `Block(SiteId,Name)`, `Apartment(BlockId,Door,Floor)`, `Dues(ApartmentId,Year,Month)` … | `.HasIndex(...).IsUnique()` | Veri bütünlüğü |

---

## Tablo Listesi (Domain → SQL)

| Entity (Domain) | Tablo (snake_case) | Kiracı? | Not |
|-----------------|--------------------|---------|-----|
| `Site` | `sites` | (anchor) | JSONB settings, text[] tags, GIN |
| `User` | `users` | SiteId nullable | Role/Plan enum, premium, refresh token |
| `Block` | `blocks` | ✅ | unique(SiteId,Name) |
| `ApartmentType` | `apartment_types` | ✅ | BaseDues, ArsaPayi |
| `Apartment` | `apartments` | ✅ | unique(BlockId,Door,Floor) |
| `Resident` | `residents` | ✅ | TcNo (FAZ 6 maskelenecek), UserId nullable |
| `Dues` | `dues` | ✅ | unique(ApartmentId,Year,Month), JSONB breakdown |
| `ExtraDues` | `extra_dues` | ✅ | Premium özellik |
| `ExtraDuesDifference` | `extra_dues_differences` | ❌ (türetilmiş) | unique(ExtraDuesId,ApartmentTypeId) |
| `FinancialTransaction` | `transactions` | ✅ | Income/Expense tek tablo, DocumentUrl→MinIO |
| `Exemption` | `exemptions` | ✅ | Daire bazlı muafiyet (DiscountRatio) |

> **Gelir/Gider kararı:** Rehberin "Incomes/Expenses" ortak şeması (Category, Amount, Date, Description, DocumentUrl) tek tabloya işaret eder → `FinancialTransaction` + `TransactionType` enum. Kod tekrarı yok, tek endpoint.

---

## EF Core Code-First Akışı (FAZ 3'te uygulanacak)

```bash
# Migration oluştur
dotnet ef migrations add InitialCreate \
  --project src/SiteYonetim.Infrastructure \
  --startup-project src/SiteYonetim.Api \
  --output-dir Persistence/Migrations

# Veritabanına uygula
dotnet ef database update --project src/SiteYonetim.Infrastructure \
  --startup-project src/SiteYonetim.Api
```

Bağlantı dizesi `deploy/.env` → `ConnectionStrings__DefaultConnection` (docker servisi `db`).
