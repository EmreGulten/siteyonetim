namespace SiteYonetim.Domain.ValueObjects;

/// <summary>
/// Siteye ait esnek ayarlar. PostgreSQL <b>JSONB</b> sütunu olarak saklanır —
/// şema değişikliği gerektirmeden yeni ayar eklenebilir, JSON anahtarları
/// üzerinden indekslenebilir (<c>->&gt;</c> operatörü ile sorgulanabilir).
/// </summary>
public class SiteSettings
{
    /// <summary>Para birimi (ISO 4217): TRY, USD, EUR ...</summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>Aidatın ayın hangi günü üretileceği (varsayılan 1).</summary>
    public int DuesGenerationDay { get; set; } = 1;

    /// <summary>Gecikme faizi oranı (örn. 0.02 = %2/ay). 0 = kapalı.</summary>
    public decimal LateFeeRate { get; set; } = 0m;

    /// <summary>Branding: makbuz üstündeki renk (hex).</summary>
    public string BrandColor { get; set; } = "#1e6f5c";

    /// <summary>Logo görselinin MinIO yolu.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Resmi makbuz üstünde görünecek yönetici imzası/unvanı.</summary>
    public string? ManagerTitle { get; set; }

    /// <summary>IBAN (aidat havale/EFT bilgisi).</summary>
    public string? Iban { get; set; }

    /// <summary>WhatsApp/destek telefonu.</summary>
    public string? SupportPhone { get; set; }
}
