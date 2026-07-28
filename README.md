# Site & Apartman Yönetim Sistemi

Enterprise SaaS — .NET Core 8 Web API · PostgreSQL · MinIO · React Native · AdMob + Premium IAP.
Docker üzerinde VPS'te çalışır. Detaylı mimari rehber: [`siteyonetim.md`](./siteyonetim.md).

## Proje Yapısı

```
SiteYönetimi/
├── siteyonetim.md                  # Faz-faz mimari rehberi
├── docs/
│   └── 01-architecture.md          # FAZ 1 mimari diyagramı
├── deploy/
│   ├── docker-compose.yml          # 4 konteyner + Nginx + Certbot
│   ├── .env.example                # Tüm ortam değişkenleri
│   ├── nginx/
│   │   ├── nginx.conf              # Reverse proxy + rate limit + gzip
│   │   └── conf.d/                 # HTTPS server blokları
│   └── scripts/
│       ├── vps-setup.sh            # VPS kurulumu (Docker + UFW + fail2ban)
│       ├── obtain-ssl.sh           # İlk Let's Encrypt sertifikası
│       └── deploy.sh               # docker compose up --build
├── backend/                        # .NET 8 Clean Architecture
│   ├── .dockerignore
│   ├── src/
│   │   ├── SiteYonetim.Api/Dockerfile
│   │   ├── SiteYonetim.Domain/     # Entity'ler, enumlar, value object (FAZ 2 ✅)
│   │   └── SiteYonetim.Infrastructure/  # DbContext + FluentAPI + tenant (FAZ 2 ✅)
│   └── tests/SiteYonetim.ModelSmokeTest/  # Model doğrulama (FAZ 2 ✅)
└── mobile/                         # React Native (FAZ 4-5)
```

## Hızlı Başlangıç (VPS)

```bash
# 1) VPS hazırlığı (root)
sudo bash deploy/scripts/vps-setup.sh

# 2) Ortam değişkenleri
cp deploy/.env.example deploy/.env
# .env içindeki parolaları GÜÇLÜ değerlerle değiştir (openssl rand -base64 32)

# 3) Sistem ayağa kalkar
cd deploy && docker compose --env-file .env up -d --build

# 4) SSL (DNS kaydı yapıldıktan sonra)
bash scripts/obtain-ssl.sh api.siten.com you@posta.com
```

## Geliştirme (yerel)

```bash
cd backend
dotnet build                                    # Tüm projeler derlenir
dotnet run --project tests/SiteYonetim.ModelSmokeTest  # Model doğrulama (DB gerekmez)
```

## İlerleme Durumu

- [x] **FAZ 1** — Altyapı, VPS ve Mimari Tasarım
- [x] **FAZ 2** — Veritabanı Tasarımı (PostgreSQL) ✅ derlendi + doğrulandı
- [x] **FAZ 3** — Backend Geliştirme (.NET Core 8) ✅ derlendi, migration üretildi
- [x] **FAZ 4** — Frontend Geliştirme (React Native)
- [x] **FAZ 5** — Ticarileştirme (AdMob + Premium IAP) ✅ backend derlendi
- [x] **FAZ 6** — DevOps, Deployment ve Güvenlik ✅ CI/CD + güvenlik

> Backend (.NET): **0 uyarı / 0 hata** derlenir, EF model smoke test geçer.
> Mobil (React Native): native modüller (chart-kit/ads/iap) `expo prebuild` + dev build ister.
