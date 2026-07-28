#!/usr/bin/env bash
# ============================================================
#  VPS kurulum scripti (FAZ 1)
#  Ubuntu 20.04+ / Debian 11+ üzerinde:
#    - Sistemi günceller
#    - Docker + Docker Compose kurar
#    - UFW firewall kurar (sadece 22, 80, 443 açık)
#    - Fail2ban kurar (SSH brute-force koruması)
#    - Swap ekler (küçük VPS'ler için)
#
#  KULLANIM (root olarak):
#    chmod +x vps-setup.sh && ./vps-setup.sh
# ============================================================
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "Bu script root olarak çalıştırılmalı: sudo ./vps-setup.sh"
  exit 1
fi

echo "==> 1/6 Sistem paketleri güncelleniyor..."
apt-get update -y
apt-get upgrade -y
apt-get install -y ca-certificates curl gnupg lsb-release ufw fail2ban \
                   apt-transport-https software-properties-common

echo "==> 2/6 Docker repository ekleniyor..."
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/"$(. /etc/os-release; echo "$ID")"/gpg \
  | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/$(. /etc/os-release; echo "$ID") \
$(lsb_release -cs) stable" > /etc/apt/sources.list.d/docker.list

apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

systemctl enable --now docker
echo "    Docker sürümü: $(docker --version)"
echo "    Compose sürümü: $(docker compose version)"

echo "==> 3/6 Firewall (UFW) kuruluyor..."
# DEFAULT politikalar
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
# Sadece gereken portlar
ufw allow 22/tcp       comment 'SSH'
ufw allow 80/tcp       comment 'HTTP (Nginx + Let'"'"'s Encrypt)'
ufw allow 443/tcp      comment 'HTTPS (Nginx)'
ufw --force enable
echo "    UFW durumu:"
ufw status verbose | sed 's/^/      /'

echo "==> 4/6 Fail2ban (SSH koruması) başlatılıyor..."
systemctl enable --now fail2ban

echo "==> 5/6 Swap alanı ekleniyor (2GB)..."
if ! swapon --show | grep -q swap; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
  echo 'vm.swappiness=10' > /etc/sysctl.d/99-swappiness.conf
  sysctl -p /etc/sysctl.d/99-swappiness.conf
  echo "    Swap etkin: $(swapon --show)"
else
  echo "    Swap zaten mevcut, atlandı."
fi

echo "==> 6/6 Kurulum tamamlandı."
cat <<'NEXT'

  ✅ VPS hazır. Sonraki adımlar:

  1) Projeyi klonla/indir ve deploy klasörüne gir.
  2) cp .env.example .env  → değerleri GÜÇLÜ parolalarla doldur.
  3) cd deploy && docker compose --env-file .env up -d --build
  4) İlk SSL sertifikası: ./scripts/obtain-ssl.sh api.siten.com you@posta.com
  5) Durum: docker compose ps  |  Loglar: docker compose logs -f api

NEXT
