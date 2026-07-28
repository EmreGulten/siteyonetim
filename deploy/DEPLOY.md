# Deploy Runbook — Site Yönetim Backend

Bu dosya, backend'in uzak sunucuya deploy edilmesi için referanstır.
Bir sonraki deploy'da **bu dosyayı oku** ve aşağıdaki adımları izle.
Her deploy'da "Deploy Geçmişi" bölümünü güncelle.

---

## 1) Sunucu Bilgileri

| Alan | Değer |
|---|---|
| Host | `62.238.36.24` |
| Kullanıcı | `root` |
| Parola | **repo'da DEĞİL** — `export VPS_ROOT_PW=...` (1Password/Keychain) veya SSH key |
| SSH | `ssh root@62.238.36.24` |
| Hostname | `fatal35` |
| Proje dizini | `/opt/siteyonetim` |
| Compose dizini | `/opt/siteyonetim/deploy` (env: `/opt/siteyonetim/deploy/.env` — **dokunma**) |

> ⚠️ **GİZLİ:** Parola bu dosyada **YOK** (B5 temizliği).
>
> **SSH anahtarı KURULDU (2026-07-28):** `~/.ssh/id_ed25519.pub` sunucuya eklendi
> (ssh-copy-id). Private key **passphrase'li** → parolasız bağlantı için (bir kez):
> `ssh-add --apple-use-keychain ~/.ssh/id_ed25519`
> Sonra tüm deploy komutları sade `ssh root@62.238.36.24 ...` ve `rsync -e ssh ...` olur
> (sshpass gerekmez). ssh-add yapılmadıysa `SSHPASS="$VPS_ROOT_PW" sshpass -e ...` hâlâ çalışır.

## 2) Ne Deploy Edilir

- **Backend:** .NET 8 API (`backend/src/SiteYonetim.Api`)
- **Çalışma şekli:** Docker Compose — servisler: `db` (PostgreSQL), `minio`, `api`, `pgadmin`
- **API image'ı:** `siteyonetim/api:latest` (sunucuda yerel olarak build edilir, registry'den pull edilmez)
- **Build context:** `../backend`, Dockerfile: `backend/src/SiteYonetim.Api/Dockerfile` (multi-stage .NET 8)
- **Dış erişim:** API host portuna **8080** olarak yayınlanır (nginx/SSL yok — dev/HTTP modu).
  Mobil uygulama bu porta SSH tüneli (`localhost:9099 → sunucu:8080`) ile bağlanır.
- **Mobil (Expo):** sunucuya deploy edilmez; yerel geliştirme uygulamasıdır.
- **Kod aktarımı:** Git yok → **rsync** (Mac'ten). Dosyalar sunucuda `501:staff` sahipliğinde.

## 3) Deploy Adımları (DOĞRULANMIŞ prosedür — bunu kullan)

> Yerel Mac'te çalıştırılır. `sshpass` `/opt/homebrew/bin/sshpass` konumunda kurulu.

### ⚠️ Kritik tuzak: bağlantı flag'leri
`sshpass` ile bağlanırken HER ZAMAN şu flag'ler şart (yoksa publickey denemesinde asılı kalır):
```
-o PreferredAuthentications=password -o PubkeyAuthentication=no -o StrictHostKeyChecking=accept-new
```
Parolayı `ps` listesinde göstermemek için `SSHPASS='<parola>' sshpass -e ...` kullan.
Bağlantı ara sıra "Permission denied" verebiliyor (geçici) → biraz bekleyip tekrar dene.

### Adım 1 — Backend'i senkronize et (SADECE `backend/`)
`deploy/.env`'e ve çalışan container'lara DOKUNMA. Sadeca kaynak kodu:
```bash
SSHPASS="$VPS_ROOT_PW" rsync -avz \
  --exclude='bin/' --exclude='obj/' --exclude='.DS_Store' --exclude='*.user' \
  -e "sshpass -e ssh -o PreferredAuthentications=password -o PubkeyAuthentication=no -o StrictHostKeyChecking=accept-new" \
  /Users/emregulten/Desktop/SiteYonetimi/backend/ \
  root@62.238.36.24:/opt/siteyonetim/backend/
```
İpucu: `rsync -avzn ...` (dry-run) ile önce nelerin gideceğini görebilirsin.

### Adım 2 — API image'ını yeniden derle + container'ı yeniden başlat
**⚠️ HER İKİ compose dosyasını kullan.** `docker-compose.dev.yml` override'ı `8080:8080`
port mapping'ini ekler. Sadece base dosyayla `up` yaparsan port yayınlanmaz, API dışarıdan
erişilemez olur ve mobil bağlantı kırılır.
```bash
SSHPASS="$VPS_ROOT_PW" sshpass -e ssh \
  -o PreferredAuthentications=password -o PubkeyAuthentication=no \
  -o StrictHostKeyChecking=accept-new root@62.238.36.24 \
  'cd /opt/siteyonetim/deploy && \
   docker compose --env-file .env -f docker-compose.yml -f docker-compose.dev.yml up -d --build api'
```
Sadece `api` rebuild edilir; `db` ve `minio` dokunulmaz. Migration açılışta otomatik
(`db.Database.Migrate()`); şema değişmediyse no-op.

## 4) Doğrulama (deploy sonrası)

```bash
# Sağlık
ssh ... 'curl -s localhost:8080/health'            # -> {"status":"ok"}

# Port mapping var mı (8080->8080 olmalı)
ssh ... 'docker port siteyonetim-api'              # -> 8080/tcp -> 0.0.0.0:8080

# Yeni uçlar var mı: 401 = uç var (auth gerekli), 404 = eski kod (deploy olmamış)
ssh ... 'curl -s -o /dev/null -w "%{http}\n" localhost:8080/api/reports/dues?year=2026'   # 401 beklenir

# Hata logu
ssh ... 'docker logs --tail=50 siteyonetim-api'
```

## 5) Deploy Geçmişi

| Tarih | Ne yapıldı | Sonuç |
|---|---|---|
| 2026-07-13 | Raporlar (8 rapor) + aidat düzenleme uçları deploy edildi. `backend/` rsync, `api` image rebuild + recreate. | ✅ Başarılı. Health OK, port 8080 map'li, tüm yeni uçlar 401 (yayında). Not: ilk `up` sadece base dosyayla yapıldı, port mapping düştü → dev override ile düzeltildi. |
| 2026-07-17 | Blok silme (`DELETE /blocks/{id}`, 409 koruması) + KMK ihtarname PDF ucu + Premium grant ucu deploy edildi. rsync + `up --build api` (iki compose dosyasıyla). | ✅ Başarılı. Health OK, port 8080 map'li, yeni uçlar 401 (yayında). |

## 6) Notlar / Tuzaklar

- **İki compose dosyası şart:** `up`/`build` her zaman `-f docker-compose.yml -f docker-compose.dev.yml` ile. Aksi halde 8080 host'a yayınlanmaz.
- **8080'e dokunma:** Mobil uygulama bu porta bağlı (SSH tüneli `localhost:9099 → sunucu:8080`). Port değiştirmek tüneli kırar.
- **deploy/.env'i ezme:** Gerçek DB/JWT/MinIO sırları orada. rsync'i sadece `backend/` ile sınırla.
- **80/443 başkasına ait:** Sunucudaki 80/443 `fitness_nginx` container'ına aittir, siteyonetim'e değil.
- **Auth flakiness:** sshpass bağlantısı ara sıra "Permission denied" verebilir; bekleyip tekrar dene. Kalıcı çözüm: SSH anahtarı kur.
- **Şema:** Bu deploy migration içermiyor (sadece uç/DTO/servis). İleride entity/kolon eklenirse yeni EF migration dosyasının da `backend/` ile gitmesi gerekir.
