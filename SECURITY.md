# Güvenlik — Site & Apartman Yönetim Sistemi

Kullanılan güvenlik önlemleri ve uygulandıkları yerler (FAZ 6).

## 1. PostgreSQL dışa kapalı ✅
- `deploy/docker-compose.yml` → `db` servisinde `ports:` **tanımsız**. PostgreSQL yalnız
  `siteyonetim-net` internal Docker network'ünden erişilebilir. Dış IP'den bağlantı imkânsız.
- pgAdmin/MinIO da port yayınlanmaz; yalnız internal/VPN.
- Tek dışa açık nokta **Nginx (80/443)**.

## 2. SQL Injection koruması ✅
- Tüm veri erişimi **EF Core LINQ** üzerinden (`ReportService`, `FinancialSummaryService`, ...).
- **Raw SQL yok** (`FromSqlRaw` kullanılmadı). Parametreli sorgu gerekse bile EF'nin parameterizasyonu.
- `CollectionService`, `DuesGenerationService` LINQ ile yazıldı.

## 3. Rate Limiting ✅ (IP başına 60/dk)
- `SiteYonetim.Api/Program.cs` → `AddRateLimiter` + `per-ip` token-bucket politikası
  (60 token / 60 saniye, `QueueLimit=0`).
- Ek katman: `deploy/nginx/nginx.conf` → `limit_req_zone` 10r/s burst 20.
- Aşım durumunda **429 Too Many Requests**.

## 4. CORS ✅
- `Program.cs` → `MobileAndPanel` politikası. İzinli kaynaklar `CORS_ALLOWED_ORIGINS`
  env'inden (örn. `https://panel.siten.com`, mobil app scheme). Joker yalnız geliştirmede.

## 5. Veri Maskelendirme (TC Kimlik No) ✅
- `ReportService.MaskTc()` → yalnızca son 4 hane görünür (`*******1234`).
- Borçlu daire listesi/raporlarında uygulanır (`DebtorApartmentDto.TcMasked`).

## 6. Kimlik Doğrulama & Yetkilendirme ✅
- **JWT** (HS256, ≥64 bayt anahtar) → claim'ler: `sub`, `email`, `role`, `site_id`.
- Access token 60 dk + refresh token 30 gün (rotasyon).
- **Role-based**: `[Authorize(Policy="SiteManager")]` → veri girişi yalnız yöneticilere.
- **Multi-tenant**: `HttpCurrentUserService` → `AppDbContext` global query filter
  (bir yönetici yalnız kendi sitesini görür; SiteId çözülemezse fail-safe boş küme).
- Brute-force kilidi: 5 başarısız deneme → 15 dk hesap kilidi.

## 7. Diğer sertleştirme
- **Soft-delete** + audit (`CreatedAt/By`, `UpdatedAt/By`) tüm tablolarda.
- **İyimser eşzamanlılık** (`xmin`) — eşzamanlı aidat güncellemelerinde çakışma.
- **Parola hash** BCrypt (work factor 12); düz metin saklanmaz.
- **MinIO**: görseller DB'de değil; GUID ile yeniden adlandırılır, public bucket read-only.
- **HTTPS zorunlu**: Nginx TLS 1.2/1.3 + HSTS; `RequireHttpsMetadata` üretimde açık.
- **Security headers**: HSTS, X-Content-Type-Options, X-Frame-Options DENY, Referrer-Policy.
- **Non-root container**: API `appuser` ile çalışır; Dockerfile multi-stage.
- **Secrets**: `.env` git'e dahil değil (`.gitignore`); store anahtarları `secrets/` volume.
- **Güvenli IAP**: satın alma doğrulaması SUNUCUDA (Google/Apple API), mobil değil.
- **VPS**: UFW (yalnız 22/80/443) + fail2ban + swap (`vps-setup.sh`).

## Dağıtım güvenliği (CI/CD)
- GitHub Actions: `production` environment — `VPS_SSH_KEY`, `VPS_HOST` secrets.
- Image: GHCR'a push, VPS'te `docker compose pull && up -d --build`.
- Health check deploy sonrası (`/health`).
