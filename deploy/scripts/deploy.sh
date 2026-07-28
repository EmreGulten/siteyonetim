#!/usr/bin/env bash
# ============================================================
#  CI/CD tarafından çağrılan dağıtım scripti (FAZ 1 iskelet)
#  VPS üzerinde:  docker compose up -d --build
# ============================================================
set -euo pipefail
cd "$(dirname "$0")/.."

ENV_FILE="${ENV_FILE:-.env}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ $ENV_FILE bulunamadı. Önce: cp .env.example .env"
  exit 1
fi

echo "==> Image'lar build ediliyor..."
docker compose --env-file "$ENV_FILE" build

echo "==> Konteynerler ayağa kaldırılıyor..."
docker compose --env-file "$ENV_FILE" up -d --remove-orphans

echo "==> Durum:"
docker compose ps
