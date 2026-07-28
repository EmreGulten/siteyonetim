using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.DTOs.Dues;

public sealed class DuesDto
{
    public Guid Id { get; set; }
    public Guid ApartmentId { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty; // "A Blok / Daire 12"
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Remaining => Amount - PaidAmount;
    public DuesStatus Status { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? ReceiptUrl { get; set; }
}

public sealed class CollectDuesRequest
{
    public Guid DuesId { get; set; }
    /// <summary>Ödenen tutar (kısmi ödeme destekli).</summary>
    public decimal Amount { get; set; }
    /// <summary>Üretilen PDF MinIO'ya kaydedilsin mi?</summary>
    public bool GenerateReceipt { get; set; } = true;
}

public sealed class CollectDuesResponse
{
    public DuesDto Dues { get; set; } = new();
    /// <summary>Makbuz PDF byte dizisi (mobil indirme için). GenerateReceipt=false ise null.</summary>
    public byte[]? ReceiptPdf { get; set; }
    public string? ReceiptUrl { get; set; }
}

public sealed class GenerateDuesRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? SiteIdOverride { get; set; } // SuperAdmin belirli site için
}

/// <summary>Tek bir aidat kaydının tutarını (override) güncellemek için.</summary>
public sealed class UpdateDuesRequest
{
    /// <summary>O aya ait yeni aidat tutarı (₺).</summary>
    public decimal Amount { get; set; }
}

public sealed class GenerateDuesResult
{
    public int ApartmentsProcessed { get; set; }
    public int DuesCreated { get; set; }
    public int Exempted { get; set; }
}
