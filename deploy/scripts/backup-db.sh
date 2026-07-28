#!/usr/bin/env bash
# ============================================================
#  PostgreSQL yedekleme (FAZ 6)
#  pg_dump ile günlük yedek alır, eski yedekleri temizler.
#  Cron: 0 3 * * *  /opt/siteyonetim/backup-db.sh
# ============================================================
set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/siteyonetim}"
KEEP_DAYS="${KEEP_DAYS:-14}"
CONTAINER_DB="${CONTAINER_DB:-siteyonetim-db}"
DB_USER="${DB_USER:-siteyonetim_app}"
DB_NAME="${DB_NAME:-siteyonetim}"
S3_BUCKET="${S3_BUCKET:-}"  # opsiyonel: aws s3 cp için

mkdir -p "$BACKUP_DIR"
STAMP=$(date -u +%Y%m%d-%H%M%S)
FILE="$BACKUP_DIR/${DB_NAME}-${STAMP}.sql.gz"

echo "==> Yedek alınıyor: $FILE"
docker exec -t "$CONTAINER_DB" pg_dump -U "$DB_USER" -d "$DB_NAME" --no-owner --clean --if-exists \
  | gzip > "$FILE"

echo "==> Sıkıştırma boyutu: $(du -h "$FILE" | cut -f1)"

# Opsiyonel: S3'e kopyala
if [[ -n "$S3_BUCKET" ]]; then
  aws s3 cp "$FILE" "s3://$S3_BUCKET/db-backups/" --quiet && echo "==> S3'e yüklendi"
fi

# Eski yedekleri temizle
find "$BACKUP_DIR" -name "${DB_NAME}-*.sql.gz" -mtime +"$KEEP_DAYS" -delete
echo "==> $KEEP_DAYS günden eski yedekler temizlendi."
echo "✅ Yedek tamam."
