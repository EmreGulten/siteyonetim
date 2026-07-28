using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Tahsilat makbuzu servisi. Dues kaydından <see cref="ReceiptData"/> oluşturur,
/// (ücretsiz plan ise watermark işaretler) ve IReceiptPdfRenderer ile PDF byte[] üretir.
/// Üretilen PDF MinIO'ya da yüklenip Dues.ReceiptUrl'e yazılır.
/// </summary>
public class ReceiptService
{
    private readonly IAppDbContext _db;
    private readonly IReceiptPdfRenderer _pdf;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserService _current;

    public ReceiptService(IAppDbContext db, IReceiptPdfRenderer pdf, IFileStorage storage, ICurrentUserService current)
    {
        _db = db;
        _pdf = pdf;
        _storage = storage;
        _current = current;
    }

    /// <summary>Makbuzu üretir. PDF byte[] döner; isterse MinIO'ya da kaydeder.</summary>
    public async Task<(byte[] Pdf, string? Url)> GenerateAsync(Guid duesId, bool persist = true, CancellationToken ct = default)
    {
        var dues = await _db.Dues
            .Include(d => d.Apartment).ThenInclude(a => a!.Block)
            .Include(d => d.Apartment).ThenInclude(a => a!.Residents)
            .FirstOrDefaultAsync(d => d.Id == duesId, ct)
            ?? throw new InvalidOperationException("Aidat kaydı bulunamadı.");

        var apt = dues.Apartment!;
        var site = await _db.Sites.FirstAsync(s => s.Id == dues.SiteId, ct);
        var resident = apt.Residents.FirstOrDefault(r => r.IsActive)
                    ?? apt.Residents.FirstOrDefault();

        var data = new ReceiptData
        {
            SiteName = site.Name,
            ManagerTitle = site.Settings.ManagerTitle,
            BrandColor = site.Settings.BrandColor,
            LogoUrl = site.Settings.LogoUrl,
            BlockName = apt.Block?.Name ?? "-",
            DoorNumber = apt.DoorNumber,
            ResidentName = resident?.FullName ?? "-",
            Year = dues.Year,
            Month = dues.Month,
            Amount = dues.Amount,
            PaidAmount = dues.PaidAmount,
            PaidDate = dues.PaidDate ?? DateTime.UtcNow,
            Currency = site.Settings.Currency,
            ReceiptNumber = $"RCP-{dues.Year}{dues.Month:D2}-{dues.Id.ToString()[..8].ToUpper()}",
            IsFreePlan = await IsUserOnFreePlanAsync(ct),
        };

        var pdf = _pdf.RenderReceipt(data);

        string? url = null;
        if (persist)
        {
            var fname = $"receipt-{data.ReceiptNumber}.pdf";
            url = await _storage.UploadBytesAsync(pdf, fname, "application/pdf", ct);
            dues.ReceiptUrl = url;
            await _db.SaveChangesAsync(ct);
        }

        return (pdf, url);
    }

    private async Task<bool> IsUserOnFreePlanAsync(CancellationToken ct)
    {
        if (_current.UserId is null) return true;
        var user = await _db.Users.FindAsync(new object?[] { _current.UserId }, ct);
        return user is null || !user.IsPremium;
    }
}
