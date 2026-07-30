using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.DTOs.Reports;

/// <summary>Aidat raporu satırı: daire bazında aylık aidat/tahsilat/kalan borç.</summary>
public sealed class DuesReportRowDto
{
    public Guid ApartmentId { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Remaining => Amount - PaidAmount;
    public DuesStatus Status { get; set; }
}

/// <summary>Ek aidat raporu satırı: kampanya + daire tipi farkı (Premium).</summary>
public sealed class ExtraDuesReportRowDto
{
    public Guid ExtraDuesId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ApartmentTypeName { get; set; }
    public decimal Amount { get; set; }
    public int InstallmentCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>Daire raporu satırı: blok/daire + malik + aylık aidat.</summary>
public sealed class ApartmentReportRowDto
{
    public Guid ApartmentId { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty;
    public string BlockName { get; set; } = string.Empty;
    public string DoorNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    public decimal MonthlyDues { get; set; }
    public bool IsOccupied { get; set; }
}

/// <summary>Gelir/Gider/Detaylı işlem raporu satırı.</summary>
public sealed class TransactionReportRowDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>Borçlu daire listesi satırı.</summary>
public sealed class DebtorApartmentDto
{
    public Guid ApartmentId { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty;
    public string? ResidentName { get; set; }
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    /// <summary>Maskelenmiş TC (örn. *******1234).</summary>
    public string? TcMasked { get; set; }
    public decimal TotalDebt { get; set; }
    public int OverdueMonths { get; set; }
}

/// <summary>KMK (Kat Mülkiyeti Kanunu) uyumlu rapor için "hazır" daireler.</summary>
public sealed class KmkReadyDto
{
    public Guid ApartmentId { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public decimal AnnualDues { get; set; }
    public decimal CollectedThisYear { get; set; }
    public bool IsKmkReady { get; set; }
    public string? Note { get; set; }
}

public sealed class ReportFilter
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public Guid? BlockId { get; set; }
    /// <summary>KMK bildirimi için borç eşiği (ay) — bu kadar ay borçlu olan "bildirim" alır.</summary>
    public int OverdueThresholdMonths { get; set; } = 3;
}

/// <summary>KMK ihtarname PDF'i için veri (Premium).</summary>
public sealed class IhtarnameData
{
    public string SiteName { get; set; } = string.Empty;
    public string? SiteAddress { get; set; }
    public string ApartmentLabel { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerTcMasked { get; set; }
    public string? Phone { get; set; }
    public int Year { get; set; }
    public List<IhtarnameDuesRow> Rows { get; set; } = new();
    public decimal TotalDebt { get; set; }
    public string? ManagerName { get; set; }
    public DateTime IssuedAt { get; set; }
}

public sealed class IhtarnameDuesRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Remaining { get; set; }
}

/// <summary>Yıllık mali bilanço PDF'i için veri (Premium).</summary>
public sealed class AnnualBalanceData
{
    public string SiteName { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<AnnualBalanceRow> Rows { get; set; } = new();
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
    public DateTime IssuedAt { get; set; }
}

public sealed class AnnualBalanceRow
{
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Income - Expense;
}
