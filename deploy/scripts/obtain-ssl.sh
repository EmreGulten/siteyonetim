#!/usr/bin/env bash
# ============================================================
#  İlk Let's Encrypt sertifikası almak için yardımcı (FAZ 1)
#  KULLANIM: ./obtain-ssl.sh <DOMAIN> <EMAIL>
#  Not: Önce DNS kaydı (DOMAIN → VPS IP) yapılmış olmalı.
# ============================================================
set -euo pipefail

DOMAIN="${1:?Kullanım: obtain-ssl.sh <DOMAIN> <EMAIL>}"
EMAIL="${2:?Kullanım: obtain-ssl.sh <DOMAIN> <EMAIL>}"

cd "$(dirname "$0")/.."

echo "==> Nginx HTTP-only modda başlatılıyor (challenge için)..."
# conf.d altındaki HTTPS bloğu geçici olarak devre dışı bırakılır
# (önce HTTP çalışmalı ki ACME http-01 challenge geçebilsin).
docker compose run --rm --entrypoint "/bin/sh" certbot \
  -c "certbot certonly --webroot -w /var/www/certbot \
      --email ${EMAIL} --agree-tos --no-eff-email \
      -d ${DOMAIN}"

echo "==> Sertifika alındı. Nginx'i tam HTTPS config ile yeniden başlat:"
echo "    1) deploy/nginx/conf.d/siteyonetim.conf.example içindeki"
echo "       'server_name _'  -> ${DOMAIN}"
echo "       'SITEDOMAIN'     -> ${DOMAIN}  olarak değiştir."
echo "    2) docker compose restart nginx"
