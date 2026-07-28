# FAZ 1 — Sistem Mimarisi, VPS ve Docker Altyapısı

> Site & Apartman Yönetim Sistemi — Enterprise SaaS mimari rehberi, Faz 1.

---

## 1.1 — Sistem Mimarisi Diyagramı

```
                            ┌──────────────────────────────────────────┐
                            │                İNTERNET                  │
                            │       (Mobil kullanıcılar, CI/CD)        │
                            └───────────────┬──────────────────────────┘
                                            │ HTTPS (443)
                                            ▼
        ┌───────────────────────────────────────────────────────────────┐
        │                       VPS (Ubuntu / Debian)                    │
        │                                                               │
        │   ┌─────────────────────────────────────────────────────┐    │
        │   │  Nginx (Reverse Proxy)          :80  → www redirect │    │
        │   │  - SSL/TLS (Let's Encrypt)      :443 → api upstream │    │
        │   │  - gzip, security headers, rate limit              │    │
        │   └───────────────────────────┬─────────────────────────┘    │
        │                               │ (docker network: siteyonetim-net) │
        │     ┌─────────────────────────┼─────────────────────────┐    │
        │     │                         ▼                           │    │
        │     │   ┌──────────────────────────────────────┐         │    │
        │     │   │  WebAPI (.NET Core 8, Kestrel) :8080 │         │    │
        │     │   │  - JWT Auth + Role-based AuthZ       │         │    │
        │     │   │  - EF Core (LINQ, raw SQL yok)       │         │    │
        │     │   │  - Hangfire (background jobs)        │         │    │
        │     │   │  - MinIO Storage Service             │         │    │
        │     │   │  - QuestPDF (makbuz üretimi)         │         │    │
        │     │   └──┬──────────────┬──────────────┬─────┘         │    │
        │     │      │              │              │               │    │
        │     │      ▼              ▼              ▼               │    │
        │     │  ┌────────┐   ┌──────────┐   ┌──────────┐          │    │
        │     │  │Postgre │   │ MinIO    │   │ pgAdmin  │          │    │
        │     │  │  :5432 │   │ :9000    │   │ :80  ⚙️  │          │    │
        │     │  │ (SQL)  │   │ (S3)     │   │ (ops)    │          │    │
        │     │  └────────┘   └──────────┘   └──────────┘          │    │
        │     │   INTERNAL   INTERNAL      internal/VPN            │    │
        │     └─────────────────────────────────────────────────────┘    │
        │                                                               │
        │   ⚠️  PostgreSQL + MinIO dış IP'ye AÇIK DEĞİL — yalnız docker   │
        │       internal network'ünden erişilebilir.                    │
        └───────────────────────────────────────────────────────────────┘

                                            ▲ HTTPS (axios + JWT)
                            ┌───────────────┴──────────────────────────┐
                            │   Mobil App (React Native / Expo)         │
                            │   - react-query (cache)                   │
                            │   - zustand (UI/token state)              │
                            │   - expo-secure-store (JWT)               │
                            │   - AdMob (banner + interstitial)         │
                            │   - react-native-iap (premium satın alma) │
                            └──────────────────────────────────────────┘
```

### Akış Özeti

| Katman | Bileşen | Sorumluluk |
|--------|---------|------------|
| İstemci | React Native | UI, AdMob reklamları, local cache (react-query) |
| Edge | Nginx | Reverse proxy, SSL (Let's Encrypt), güvenlik header'ları |
| Uygulama | .NET Core 8 WebAPI | İş mantığı, JWT auth, rol tabanlı yetki, background jobs |
| Veri | PostgreSQL | İlişkisel veri, multi-tenancy (SiteId) |
| Depolama | MinIO | Makbuz/fatura görselleri (S3 uyumlu nesne depolama) |
| Ops | pgAdmin | (Opsiyonel) veritabanı yönetimi — VPN/internal |

### Neden bu mimari?

- **Multi-tenancy tek DB + SiteId**: Tüm tablolarda `SiteId` FK → bir yönetici yalnızca kendi sitesini görür (satır seviyesi izolasyon). JWT içine `SiteId` + `Role` claim gömülür; her sorgu otomatik olarak filtreden geçer.
- **MinIO ayrı katman**: Görseller DB'ye yazılmaz; GUID ile MinIO'ya yüklenir, DB'de yalnızca URL saklanır.
- **İçeriden izole veri tabanı**: PostgreSQL portu asla dışa yayınlanmaz → dış saldırı yüzeyi sıfır.
- **PDF sunucu taraflı**: Makbuz şablonu (QuestPDF) backend'de üretilir, mobil yalnızca byte[] indirir.

---

## 1.2 — Konteyner Topolojisi (docker-compose)

4 + 1 konteyner:

1. **db** — `postgres:16-alpine` (port yayınlanmaz, internal-only)
2. **api** — `.NET Core 8` (build edilir, `:8080`)
3. **minio** — `minio/minio` (S3 API `:9000`, console `:9001` — internal)
4. **pgadmin** — `dpage/pgadmin4` (ops, internal/VPN)
5. **nginx** — `nginx:1.27-alpine` (tek dışa açık nokta: 80/443)
6. *(opsiyonel)* **certbot** — Let's Encrypt sertifika yenileme

> Detaylar: `deploy/docker-compose.yml`, `deploy/nginx/nginx.conf`
