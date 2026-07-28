# FAZ 6 — DevOps, Deployment ve Güvenlik

## 6.1 — CI/CD Pipeline (GitHub Actions)

`.github/workflows/ci-cd.yml` — `main`'e push'ta 3 aşamalı:

```
 test           build              deploy
   │              │                  │
   ▼              ▼                  ▼
 dotnet restore docker build      appleboy/ssh-action
 dotnet build   (Dockerfile)      → VPS'te:
 EF smoke test  push → GHCR         git pull
 dotnet test                       docker compose pull
                                   docker compose up -d --build
                                   curl /health (doğrulama)
```

**Gereken GitHub secrets (production environment):**
| Secret | Açıklama |
|--------|----------|
| `VPS_HOST` | Sunucu IP/domain |
| `VPS_USER` | SSH kullanıcı (deploy kullanıcısı) |
| `VPS_SSH_KEY` | SSH özel anahtarı |
| `VPS_PORT` | (ops.) SSH portu |
| `VPS_DEPLOY_DIR` | Repo yolu (`/opt/siteyonetim`) |
| `GITHUB_TOKEN` | (otomatik) GHCR için |

Image: `ghcr.io/<org>/<repo>/api:<sha>` — imza/scan eklenebilir (Trivy).

## 6.2 — Güvenlik (uygulandığı yerler)

> Ayrıntılı kontrol listesi: [`SECURITY.md`](../SECURITY.md)

| Önlem | Durum | Nerede |
|-------|-------|--------|
| PostgreSQL dışa kapalı | ✅ | `docker-compose.yml` (ports yok) |
| SQL Injection (yalnız LINQ) | ✅ | Tüm servisler — raw SQL yok |
| Rate limiting (60/dk) | ✅ | `Program.cs` per-ip + Nginx |
| CORS (izinli kaynaklar) | ✅ | `Program.cs` MobileAndPanel |
| TC maskelendirme | ✅ | `ReportService.MaskTc()` |
| JWT + role + multi-tenant | ✅ | Identity + global query filter |
| Brute-force kilidi | ✅ | `AuthService` (5 deneme / 15 dk) |
| HTTPS + HSTS + headers | ✅ | Nginx + `RequireHttpsMetadata` |
| Non-root container | ✅ | Dockerfile (appuser) |
| Backup | ✅ | `deploy/scripts/backup-db.sh` |

## Operasyonel

- **Yedekleme**: `backup-db.sh` (pg_dump → gzip → opsiyonel S3, 14 gün tut). Cron: `0 3 * * *`.
- **Loglama**: Nginx access/error log; API structured logging; Hangfire dashboard (`/hangfire`, SuperAdmin).
- **İzleme**: `/health` endpoint; CI deploy sonrası otomatik health check.
- **Sertifika**: Let's Encrypt + certbot container, günde 2x yenileme.
- **Güncelleme**: `docker compose pull && up -d --build` ile sıfır kesinti yaklaşımlı.
