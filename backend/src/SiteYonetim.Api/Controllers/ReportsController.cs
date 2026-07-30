using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Application.DTOs.Reports;
using SiteYonetim.Application.Services;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("per-ip")]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    private readonly PremiumPolicy _policy;
    public ReportsController(ReportService reports, PremiumPolicy policy)
    {
        _reports = reports;
        _policy = policy;
    }

    // ─── Borçlu daireler ─────────────────────────────────────────────────
    /// <summary>Borçlu daireler listesi (TC maskeli). Misafir erişebilir (boş döner).</summary>
    [HttpGet("debtors")]
    [AllowAnonymous]
    public async Task<IActionResult> Debtors([FromQuery] int year, [FromQuery] int? month,
        [FromQuery] Guid? blockId = null, CancellationToken ct = default)
        => Ok(await _reports.GetDebtorsAsync(new ReportFilter { Year = year, Month = month, BlockId = blockId }, ct));

    /// <summary>Borçlu listesi CSV (Excel-uyumlu) olarak indir.</summary>
    [HttpGet("debtors/export")]
    public async Task<IActionResult> ExportDebtorsCsv([FromQuery] int year, [FromQuery] int? month, CancellationToken ct)
    {
        var rows = await _reports.GetDebtorsAsync(new ReportFilter { Year = year, Month = month }, ct);
        var csv = Csv("Daire;Sakin;Telefon;Borc(TL);GecikmeAy", rows.Select(r =>
            $"{r.ApartmentLabel};{r.ResidentName};{r.Phone};{Fmt(r.TotalDebt)};{r.OverdueMonths}"));
        return File(Utf8(csv), "text/csv; charset=utf-8", $"borclular-{year}.csv");
    }

    // ─── KMK / hazır olanlar (Premium) ───────────────────────────────────
    /// <summary>KMK uyumlu "bildirim için hazır" daireler (Premium).</summary>
    [HttpGet("kmk")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Kmk([FromQuery] int year,
        [FromQuery] int overdueThresholdMonths = 3, CancellationToken ct = default)
        => Ok(await _reports.GetKmkReportAsync(
            new ReportFilter { Year = year, OverdueThresholdMonths = overdueThresholdMonths }, ct));

    /// <summary>Hazır olanlar listesi CSV.</summary>
    [HttpGet("kmk/export")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> ExportKmkCsv([FromQuery] int year,
        [FromQuery] int overdueThresholdMonths = 3, CancellationToken ct = default)
    {
        var rows = await _reports.GetKmkReportAsync(
            new ReportFilter { Year = year, OverdueThresholdMonths = overdueThresholdMonths }, ct);
        var csv = Csv("Daire;Malik;YillikAidat;Tahsil;Durum", rows.Select(r =>
            $"{r.ApartmentLabel};{r.OwnerName};{Fmt(r.AnnualDues)};{Fmt(r.CollectedThisYear)};{r.Note}"));
        return File(Utf8(csv), "text/csv; charset=utf-8", $"hazir-olanlar-{year}.csv");
    }

    /// <summary>KMK ihtarname PDF'i üretir (Premium). Belirli daire + yıl için.</summary>
    [HttpGet("kmk/{apartmentId:guid}/ihtarname")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Ihtarname(Guid apartmentId, [FromQuery] int year, CancellationToken ct)
    {
        if (!await _policy.IsPremiumAsync(ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "İhtarname üretimi bir Premium özelliktir." });
        if (year <= 0) year = DateTime.UtcNow.Year;
        var pdf = await _reports.GetIhtarnamePdfAsync(apartmentId, year, ct);
        return File(pdf, "application/pdf", $"ihtarname-{apartmentId}-{year}.pdf");
    }

    // ─── Yıllık bilanço PDF (Premium) ───────────────────────────────────
    /// <summary>Yıllık mali bilanço PDF'i (gelir/gider/net, aylık döküm). Premium.</summary>
    [HttpGet("balance/{year:int}/pdf")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> BalancePdf(int year, CancellationToken ct)
    {
        if (!await _policy.IsPremiumAsync(ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Yıllık bilanço PDF bir Premium özelliktir." });
        var pdf = await _reports.GetAnnualBalancePdfAsync(year, ct);
        return File(pdf, "application/pdf", $"bilanco-{year}.pdf");
    }

    // ─── Veri yedekleme ZIP (Premium) ────────────────────────────────────
    /// <summary>Tüm site verisinin JSON ZIP yedeği. Premium.</summary>
    [HttpGet("backup")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Backup(CancellationToken ct)
    {
        if (!await _policy.IsPremiumAsync(ct))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Veri yedekleme bir Premium özelliktir." });
        var zip = await _reports.GetBackupZipAsync(ct);
        return File(zip, "application/zip", $"site-yedek-{DateTime.UtcNow:yyyyMMdd}.zip");
    }

    // ─── Aidat raporu ────────────────────────────────────────────────────
    /// <summary>Aidat raporu: yıla (opsiyonel ay/blok) göre aidat kalemleri. Misafir erişebilir.</summary>
    [HttpGet("dues")]
    [AllowAnonymous]
    public async Task<IActionResult> Dues([FromQuery] int year, [FromQuery] int? month,
        [FromQuery] Guid? blockId = null, CancellationToken ct = default)
        => Ok(await _reports.GetDuesReportAsync(new ReportFilter { Year = year, Month = month, BlockId = blockId }, ct));

    [HttpGet("dues/export")]
    public async Task<IActionResult> ExportDuesCsv([FromQuery] int year, [FromQuery] int? month,
        [FromQuery] Guid? blockId = null, CancellationToken ct = default)
    {
        var rows = await _reports.GetDuesReportAsync(new ReportFilter { Year = year, Month = month, BlockId = blockId }, ct);
        var csv = Csv("Daire;Yil;Ay;Aidat;Tahsil;Kalan;Durum", rows.Select(r =>
            $"{r.ApartmentLabel};{r.Year};{r.Month};{Fmt(r.Amount)};{Fmt(r.PaidAmount)};{Fmt(r.Remaining)};{r.Status}"));
        return File(Utf8(csv), "text/csv; charset=utf-8", $"aidat-{year}.csv");
    }

    // ─── Ek aidat raporu (Premium) ───────────────────────────────────────
    /// <summary>Ek aidat kampanyaları + daire tipi farkları. Misafir erişebilir.</summary>
    [HttpGet("extra-dues")]
    [AllowAnonymous]
    public async Task<IActionResult> ExtraDues(CancellationToken ct = default)
        => Ok(await _reports.GetExtraDuesReportAsync(ct));

    [HttpGet("extra-dues/export")]
    public async Task<IActionResult> ExportExtraDuesCsv(CancellationToken ct = default)
    {
        var rows = await _reports.GetExtraDuesReportAsync(ct);
        var csv = Csv("Kampanya;DaireTipi;Taksit;Tutar;Baslangic;Bitis;Durum", rows.Select(r =>
            $"{r.Title};{r.ApartmentTypeName};{r.InstallmentCount};{Fmt(r.Amount)};{r.StartDate:yyyy-MM-dd};{r.EndDate:yyyy-MM-dd};{(r.IsActive ? "Aktif" : "Pasif")}"));
        return File(Utf8(csv), "text/csv; charset=utf-8", $"ek-aidat.csv");
    }

    // ─── Daire raporu ───────────────────────────────────────────────────
    /// <summary>Daire raporu: bağımsız bölümler listesi. Misafir erişebilir.</summary>
    [HttpGet("apartments")]
    [AllowAnonymous]
    public async Task<IActionResult> Apartments([FromQuery] Guid? blockId = null, CancellationToken ct = default)
        => Ok(await _reports.GetApartmentReportAsync(blockId, ct));

    [HttpGet("apartments/export")]
    public async Task<IActionResult> ExportApartmentsCsv([FromQuery] Guid? blockId = null, CancellationToken ct = default)
    {
        var rows = await _reports.GetApartmentReportAsync(blockId, ct);
        var csv = Csv("Blok;Daire;Kat;Malik;Telefon;AylikAidat;Durum", rows.Select(r =>
            $"{r.BlockName};{r.DoorNumber};{r.Floor};{r.OwnerName};{r.Phone};{Fmt(r.MonthlyDues)};{(r.IsOccupied ? "Dolu" : "Bos")}"));
        return File(Utf8(csv), "text/csv; charset=utf-8", $"daireler.csv");
    }

    // ─── Gelir / Gider / Detaylı işlem raporu ───────────────────────────
    /// <summary>Gelir raporu (TransactionType=Income). Misafir erişebilir.</summary>
    [HttpGet("income")]
    [AllowAnonymous]
    public async Task<IActionResult> Income([FromQuery] int year = 0, CancellationToken ct = default)
        => Ok(await _reports.GetTransactionsReportAsync(TransactionType.Income, year, ct));

    [HttpGet("income/export")]
    public async Task<IActionResult> ExportIncomeCsv([FromQuery] int year = 0, CancellationToken ct = default)
    {
        var csv = await TransactionsCsv(TransactionType.Income, year, ct);
        return File(Utf8(csv), "text/csv; charset=utf-8", year > 0 ? $"gelir-{year}.csv" : "gelir.csv");
    }

    /// <summary>Gider raporu (TransactionType=Expense). Misafir erişebilir.</summary>
    [HttpGet("expenses")]
    [AllowAnonymous]
    public async Task<IActionResult> Expenses([FromQuery] int year = 0, CancellationToken ct = default)
        => Ok(await _reports.GetTransactionsReportAsync(TransactionType.Expense, year, ct));

    [HttpGet("expenses/export")]
    public async Task<IActionResult> ExportExpensesCsv([FromQuery] int year = 0, CancellationToken ct = default)
    {
        var csv = await TransactionsCsv(TransactionType.Expense, year, ct);
        return File(Utf8(csv), "text/csv; charset=utf-8", year > 0 ? $"gider-{year}.csv" : "gider.csv");
    }

    /// <summary>Detaylı işlem raporu (gelir + gider tümü). Misafir erişebilir.</summary>
    [HttpGet("transactions")]
    [AllowAnonymous]
    public async Task<IActionResult> Transactions([FromQuery] int year = 0, CancellationToken ct = default)
        => Ok(await _reports.GetTransactionsReportAsync(null, year, ct));

    [HttpGet("transactions/export")]
    public async Task<IActionResult> ExportTransactionsCsv([FromQuery] int year = 0, CancellationToken ct = default)
    {
        var csv = await TransactionsCsv(null, year, ct);
        return File(Utf8(csv), "text/csv; charset=utf-8", year > 0 ? $"islem-{year}.csv" : "islem.csv");
    }

    // ─── CSV yardımcıları ───────────────────────────────────────────────
    private async Task<string> TransactionsCsv(TransactionType? type, int year, CancellationToken ct)
    {
        var rows = await _reports.GetTransactionsReportAsync(type, year, ct);
        return Csv("Tarih;Tur;Kategori;Aciklama;Tutar", rows.Select(r =>
            $"{r.Date:yyyy-MM-dd};{(r.Type == TransactionType.Income ? "Gelir" : "Gider")};{r.Category};{r.Description};{Fmt(r.Amount)}"));
    }

    /// <summary>Başlık + satırlardan noktalı-virgül ayrılımlı CSV metni üretir.</summary>
    private static string Csv(string header, IEnumerable<string> rows)
        => header + "\n" + string.Join("\n", rows);

    /// <summary>UTF-8 BOM ekleyerek Excel'de Türkçe karakter sorununu önler.</summary>
    private static byte[] Utf8(string csv)
    {
        // Excel'in UTF-8'i doğru tanıması için BOM (EF BB BF) eklenir.
        return Encoding.UTF8.GetBytes("﻿" + csv);
    }

    private static string Fmt(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
}
