Site ve Apartman Yönetim Sistemi - Enterprise SaaS Mimari Rehberi
Rol: Kıdemli Full-Stack Yazılım Mimarısı (.NET Core & React Native Uzmanı)
Hedef: VPS üzerinde Docker ile çalışacak, PostgreSQL kullanan, React Native mobil istemciye sahip, AdMob ve Premium üyelik altyapısı kurulu bir Site Yönetim Sistemi kurmak.
Talimat: GLM 5.2, aşağıdaki adımları sırasıyla oku. Her fazı tamamen anla ve sadece o faza ait kodu/çıktıyı üret. Bir adımı bitirmeden diğerine geçme.

🏗️ FAZ 1: Altyapı, VPS ve Mimari Tasarım
Adım 1.1: Sistem Mimarisi Diyagramı Mantığı
Görev: İlk olarak aklında şu akışı canlandır:

Mobile App (React Native): Kullanıcı arayüzü, AdMob reklamları, local cache.
VPS (Ubuntu/Debian): Uygulamanın beyni.
Docker Compose: VPS üzerinde 4 konteyner çalışacak: 1. WebAPI (.NET Core), 2. PostgreSQL, 3. pgAdmin (Opsiyonel yönetim için), 4. MinIO (Makbuz ve fatura görselleri için S3 uyumlu nesne depolama).
Nginx (Reverse Proxy): VPS üzerinde 80/443 portlarını .NET Core API'ye yönlendirecek, SSL sertifikalarını (Let's Encrypt) yönetecek.
Adım 1.2: VPS ve Docker Kurulum Adımları
Görev: VPS'e bağlanıp sistemi ayağa kaldıracak bash komutlarını ve docker-compose.yml dosyasını yaz.

PostgreSQL için environment variables belirle (POSTGRES_DB, POSTGRES_USER, POSTGRES_PASSWORD).
MinIO için access/secret key tanımla.
.NET Core API projesi için Dockerfile oluştur (SDK ve Runtime image kullanarak).
🗄️ FAZ 2: Veritabanı Tasarımı (PostgreSQL)
Adım 2.1: Çok Kiracılı (Multi-Tenancy) Yapı
Görev: Sistem birden fazla site/apartman yöneticisi tarafından kullanılacağı için Sites tablosu ana bağlayıcı olacaktır. Tüm tablolara SiteId (Foreign Key) ekle.

Adım 2.2: PostgreSQL Tablo ve İlişki Tasarımı (Entity Framework Core Code-First)
Görev: .NET Core tarafında kullanılacak EF Core Entity class'larını ve FluentAPI ilişkilerini yaz. Dikkat: SQLite'dan farklı olarak PostgreSQL avantajlarını (Array veri tipleri, JSONB, tam destekli ilişkisel sorgulama) kullan.

Users (Kullanıcılar): Id, Email, PasswordHash, Role (Enum: SuperAdmin, SiteManager, Resident), SiteId.
Blocks (Bloklar): Id, SiteId, Name.
ApartmentTypes (Daire Tipleri): Id, SiteId, Name, BaseDues, ArsapPayi.
Apartments (Daireler): Id, BlockId, ApartmentTypeId, DoorNumber, Floor.
Residents (Sakinler): Id, ApartmentId, UserId (Nullable, eğer uygulama kullanıyorsa), FullName, TcNo, Phone, IsOwner, IsTenant.
Dues (Aidatlar): Id, ApartmentId, Year, Month, Amount, Status (Enum: Unpaid, PartiallyPaid, Paid), PaidDate, PaidAmount.
ExtraDues (Ek Aidatlar): Id, SiteId, Title, StartDate, EndDate, InstallmentCount.
ExtraDuesDifferences (Ek Aidat Tip Farkları): Id, ExtraDuesId, ApartmentTypeId, Amount.
Incomes/Expenses (Gelir/Gider): Id, SiteId, Category, Amount, Date, Description, DocumentUrl (MinIO path).
Exemptions (Muafiyetler): Id, ApartmentId, StartDate, EndDate.
⚙️ FAZ 3: Backend Geliştirme (.NET Core 8 Web API)
Adım 3.1: Proje Yapısı (Clean Architecture)
Görev: .NET Core projesini katmanlı mimari ile oluştur: WebAPI, Application, Domain, Infrastructure.

JWT Authentication: Kullanıcı girişi için token üretimi kur. Token içine SiteId ve Role claims olarak eklensin.
Role Based Authorization: Sadece "SiteManager" rolündekiler daire ekleyebilsin, "Resident" sadece kendi daireini görsün.
Adım 3.2: Kritik İş Mantığı Servisleri (C#)
Görev: Application katmanında şu servisleri yaz:

DuesGenerationService: Ayın 1'inde çalışacak bir Background Job (Hangfire veya Worker Service) veya manuel tetiklemeli endpoint. Sadece o SiteId'ye ait daireleri çeker, DaireTipine göre Aidatı hesaplar, Exemptions tablosuna bakar (muaf ise 0 yazar), ExtraDues varsa onu da ekler ve Dues tablosuna kaydeder.
FinancialSummaryService: Dashboard için: Beklenen Gelir, Tahsil edilen, Giderler, Net Bakiye hesaplamalarını SQL Aggregate fonksiyonları ile veritabanında yapıp API'ye döndür.
ReportService: Borçlu Daireler listesi, KMK uyumlu Hazır Olanlar listesi gibi filtreli sorguları DTO'lar (Data Transfer Objects) halinde hazırla.
Adım 3.3: Dosya Yönetimi (MinIO Entegrasyonu)
Görev: Gider veya gelir eklenirken yüklenen fotoğraflar/faturalar doğrudan veritabanına kaydedilmemelidir. MinIO için bir servis yaz. Dosyayı al, GUID ile yeniden adlandır, MinIO'ya yükle ve veritabanına sadece URL'sini kaydet.

Adım 3.4: Makbuz Tasarımı ve Sunucu Tarafında PDF Üretimi
Görev: Mobilde HTML template tutmak yerine, .NET Core tarafında QuestPDF veya DinkToPdf kütüphanelerinden birini kullan.

Tahsilat makbuzu için C# class'ı oluştur (Site adı, yönetici adı, daire no, tutar vb.).
Bu class'ı alıp arka planda profesyonel bir PDF'e çeviren endpoint'i yaz (/api/receipts/generate/{duesId}).
Mobil uygulama bu endpoint'i çağırıp byte dizisi (PDF) olarak indirsin.
📱 FAZ 4: Frontend Geliştirme (React Native)
Adım 4.1: State ve Network Yönetimi
Görev: Mobil taraf için şu kütüphaneleri kur: axios (HTTP istekleri), @tanstack/react-query (Veri önbellekleme, senkronizasyon ve loading/error state yönetimi), zustand (UI state ve token yönetimi), expo-secure-store (JWT token'ı güvenli saklama).

Adım 4.2: Auth Flow ve Multi-Tenancy Kontrolü
Görev:

Login ekranı yap. API'den dönen JWT token'ı SecureStore'a kaydet.
Axios interceptor yaz: Tüm isteklere Authorization: Bearer <token> header'ı ekle. Token süresi dolarsa kullanıcıyı otomatik logout yap.
Uygulama açıldığında token'daki SiteId'yi oku ve tüm API isteklerinde bu SiteId'yi query parameter veya header olarak gönder (Böylece yönetici sadece kendi sitesinin verisini görsün).
Adım 4.3: Odaklanmış Mobil Ekranlar
Görev: Eskisi gibi her şeyi tek ekrana sıkıştırmak yerine mobil UX'e uygun tasarla:

Dashboard (Home): Bakiye kartları (Gelir/Gider/Borç), Son 5 işlem, ve "Bu Ayın Tahsilat Oranı" (Dairesel Grafik - react-native-chart-kit) göster.
Daireler: Blok bazlı filtreleme ile listeleme. Detay sayfasında o dairenin aylık aidat grafiği.
Hızlı Tahsilat: Barkod okuyucu (veya manuel giriş) ile daireyi bul, tutarı gir, anında "Ödendi" yap ve PDF makbuzu indir.
Raporlar: Excel ve PDF indirme butonları. İndirme işlemi sırasında progress bar göster.
💰 FAZ 5: Ticarileştirme - AdMob ve Premium Üyelik (Kritik Adım)
Adım 5.1: Ücretsiz vs Premium Özellik Haritası
Görev: Sistemi kısıtlamak için kuralları belirle:

Ücretsiz Plan: Tek Site, Maksimum 20 Daire, Aidat ve Gider ekleyebilir. Raporlarda AdMob Reklamı görür. Makbuzda "Ücretsiz Plan ile Oluşturuldu" watermark'ı olur.
Premium Plan: Sınırsız Site ve Daire, Ek Aidat Yönetimi, Reklamsız deneyim, KMK Uyumlu Detaylı Raporlar, Özel Branded Makbuzlar, Öncelikli Destek.
Adım 5.2: React Native AdMob Entegrasyonu
Görev: react-native-google-mobile-ads kurulumunu yap.

Banner Reklamlar: Raporlar listesinin altına ve Dashboard'un en altına sabit banner ekle.
Interstitial Reklamlar (Geçiş Reklamları): Kullanıcı "Makbuz Oluştur" veya "Excel İndir" butonuna bastığında, işlem yapılmadan önce tam ekran reklam göster. Reklam kapatıldıktan sonra işlemi gerçekleştir.
Adım 5.3: Güvenli Premium Satın Alma (In-App Purchasing)
Görev: Sadece mobilde satın alımı kontrol etmek hacklenebilirdir. Güvenli mimari şudur:

React Native tarafında react-native-iap kullan. Google Play / App Store'dan satın alma receipt'ini (fişini) al.
Bu receipt'i .NET Core Backend'e gönder (/api/subscription/verify).
.NET Core Backend: Google Play Developer API veya Apple App Store Server API'ye bağlanıp bu fişin gerçekten ödendiğini ve fraudulent (dolandırıcılık) olmadığını doğrula.
Doğrulanırsa, PostgreSQL Users tablosunda kullanıcının IsPremium = true ve PremiumExpiryDate alanlarını güncelle.
Mobil uygulama her açıldığında kullanıcının bilgilerini çekip IsPremium durumuna göre UI'ı (reklamları, butonları) güncellesin.
🚀 FAZ 6: DevOps, Deployment ve Güvenlik
Adım 6.1: VPS Üzerinde CI/CD ve Yayınlama
Görev:

GitHub Actions veya GitLab CI kullanarak bir pipeline yaz.
Kod push edildiğinde: Testleri çalıştır -> .NET Core projesini Docker image olarak build et -> VPS'e SSH ile bağlanıp docker-compose up -d --build komutunu çalıştır.
Adım 6.2: Güvenlik Önlemleri (Cyber Security)
Görev: Şunları uyguladığını kodda göster:

PostgreSQL VPS dışına (dış IP'ye) kapalı olmalı, sadece Docker internal network'ünden erişilebilir olmalı.
.NET Core API'de SQL Injection önlemi için sadece EF Core LINQ sorguları kullanılacak (Raw SQL yok).
Rate Limiting: Bir IP'nin 1 dakikada maksimum 60 istek yapmasını sağla (IP rate limiting middleware).
CORS Politikası: Sadece mobil uygulamanın veya belirli domainlerin API'ye istek atmasına izin ver.
Veri Masking: Kullanıcı listeleri çekilirken TC Kimlik No sadece son 4 hanesi görünecek şekilde maskelensin (Örn: *******1234).
