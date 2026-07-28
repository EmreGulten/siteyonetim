namespace SiteYonetim.Application.Abstractions;

/// <summary>Sunucu taraflı makbuz PDF üretimi (QuestPDF).</summary>
public interface IReceiptPdfRenderer
{
    /// <summary>Tahsilat makbuzunu profesyonel PDF'e çevirir (byte[] döner).</summary>
    byte[] RenderReceipt(ReceiptData data);
}

/// <summary>PDF şablonuna giren veri.</summary>
public sealed class ReceiptData
{
    public string SiteName { get; set; } = string.Empty;
    public string? ManagerTitle { get; set; }
    public string? BrandColor { get; set; }
    public string? LogoUrl { get; set; }
    public string DoorNumber { get; set; } = string.Empty;
    public string BlockName { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime PaidDate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? ReceiptNumber { get; set; }
    public bool IsFreePlan { get; set; } // ücretsiz plan watermark'ı
}
