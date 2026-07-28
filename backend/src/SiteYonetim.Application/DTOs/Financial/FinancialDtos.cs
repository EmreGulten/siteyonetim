using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.DTOs.Financial;

/// <summary>Dashboard finansal özet (SQL aggregate ile hesaplanır).</summary>
public sealed class FinancialSummaryDto
{
    /// <summary>Beklenen gelir (tüm aidatlar toplamı).</summary>
    public decimal ExpectedIncome { get; set; }

    /// <summary>Tahsil edilen.</summary>
    public decimal Collected { get; set; }

    /// <summary>Kalan/bekleyen borç.</summary>
    public decimal Outstanding { get; set; }

    /// <summary>Giderler toplamı.</summary>
    public decimal Expenses { get; set; }

    /// <summary>Diğer gelirler (aidat dışı).</summary>
    public decimal OtherIncome { get; set; }

    /// <summary>Net bakiye = Tahsil edilen - Giderler.</summary>
    public decimal NetBalance { get; set; }

    /// <summary>Bu ay tahsilat oranı (%) = Collected / ExpectedIncome * 100.</summary>
    public decimal CollectionRate { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
}

public sealed class TransactionDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? DocumentUrl { get; set; }
}

public sealed class CreateTransactionRequest
{
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
