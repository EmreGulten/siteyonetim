using SiteYonetim.Domain.Common;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gelir/Gider kaydı (tek tablo, <see cref="TransactionType"/> ile ayrıştırılır).
/// Rehberin "Incomes/Expenses" satırındaki ortak şema bunu tarif eder.
/// <see cref="DocumentUrl"/> MinIO yoludur — görsel/fatura DB'ye yazılmaz (FAZ 3).
/// </summary>
public class FinancialTransaction : TenantEntity
{
    public TransactionType Type { get; set; }

    /// <summary>Kategori (örn. "Elektrik", "Su", "Personel", "Onarım").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Alt başlık / açıklama.</summary>
    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    /// <summary>İlgili fatura/makbuz görselinin MinIO yolu (varsa).</summary>
    public string? DocumentUrl { get; set; }

    /// <summary>İlgili aidat ödemesi (gelirse aidat tahsilatı olarak işaretli ise).</summary>
    public Guid? RelatedDuesId { get; set; }

    public Guid? CreatedByUser { get; set; }

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public Site? Site { get; set; }
}
