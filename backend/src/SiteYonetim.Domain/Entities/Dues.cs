using SiteYonetim.Domain.Common;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Aylık aidat kaydı. <see cref="DuesGenerationService"/> (FAZ 3) tarafından
/// ayın 1'inde daire tipine + muafiyet + ek aidat farklarına göre hesaplanıp üretilir.
/// </summary>
public class Dues : TenantEntity
{
    public Guid ApartmentId { get; set; }
    public Apartment? Apartment { get; set; }

    public int Year { get; set; }

    /// <summary>1-12 arası ay.</summary>
    public int Month { get; set; }

    /// <summary>O dönem için hesaplanan toplam aidat (temel + ek - muafiyet).</summary>
    public decimal Amount { get; set; }

    /// <summary>Şu ana kadar tahsil edilen tutar.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Kalan borç = Amount - PaidAmount (hesaplanmış).</summary>
    public decimal RemainingAmount => Amount - PaidAmount;

    public DuesStatus Status { get; set; } = DuesStatus.Unpaid;

    public DateTime? PaidDate { get; set; }

    /// <summary>Son tam ödeme tarihi (gecikme faizi için — site ayarından).</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Tahsilat makbuzunun MinIO yolu (QuestPDF çıktısı).</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Üretildiği ek aidat dökümü (denetim/şeffaflık için JSONB).</summary>
    public Dictionary<string, decimal>? Breakdown { get; set; }

    /// <summary>Durumu ödeme bilgilerinden türetir.</summary>
    public void RecalculateStatus()
    {
        Status = PaidAmount <= 0
            ? DuesStatus.Unpaid
            : PaidAmount >= Amount
                ? DuesStatus.Paid
                : DuesStatus.PartiallyPaid;
    }
}
