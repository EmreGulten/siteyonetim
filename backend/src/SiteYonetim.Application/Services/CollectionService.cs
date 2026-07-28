using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Dues;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Tahsilat servisi. Ödeme alır (kısmi ödeme destekli), durumu günceller,
/// isteğe göre PDF makbuz üretir ve gelir kaydı oluşturur.
/// </summary>
public class CollectionService
{
    private readonly IAppDbContext _db;
    private readonly ReceiptService _receipts;

    public CollectionService(IAppDbContext db, ReceiptService receipts)
    {
        _db = db;
        _receipts = receipts;
    }

    public async Task<CollectDuesResponse> CollectAsync(CollectDuesRequest req, CancellationToken ct = default)
    {
        if (req.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(req.Amount), "Tutar 0'dan büyük olmalı.");

        var dues = await _db.Dues.FirstOrDefaultAsync(d => d.Id == req.DuesId, ct)
            ?? throw new InvalidOperationException("Aidat kaydı bulunamadı.");

        dues.PaidAmount = Math.Min(dues.PaidAmount + req.Amount, dues.Amount);
        if (dues.PaidDate == null) dues.PaidDate = DateTime.UtcNow;
        dues.RecalculateStatus();

        // Gelir kaydı (aidat tahsilatı)
        _db.Transactions.Add(new FinancialTransaction
        {
            Type = TransactionType.Income,
            Category = "Aidat Tahsilatı",
            Description = $"{dues.Year}-{dues.Month:D2} aidat ödemesi",
            Amount = req.Amount,
            Date = DateTime.UtcNow,
            SiteId = dues.SiteId,
            RelatedDuesId = dues.Id,
        });

        await _db.SaveChangesAsync(ct);

        byte[]? pdf = null;
        string? url = null;
        if (req.GenerateReceipt)
            (pdf, url) = await _receipts.GenerateAsync(dues.Id, persist: true, ct);

        return new CollectDuesResponse
        {
            Dues = Map(dues),
            ReceiptPdf = pdf,
            ReceiptUrl = url,
        };
    }

    /// <summary>
    /// Tek bir aidat kaydının tutarını günceller (manuel override). Daha önce
    /// tahsil edilen tutar yeni tutarı aşıyorsa törpülenir (kalan borç negatif olmasın),
    /// ardından durum yeniden hesaplanır. Üretim formülünü (base/extra/muafiyet) ezdiği
    /// için yalnızca yönetici tarafından kullanılır.
    /// </summary>
    public async Task<DuesDto> UpdateAmountAsync(Guid duesId, decimal newAmount, CancellationToken ct = default)
    {
        if (newAmount < 0) throw new ArgumentOutOfRangeException(nameof(newAmount), "Tutar negatif olamaz.");

        var dues = await _db.Dues.FirstOrDefaultAsync(d => d.Id == duesId, ct)
            ?? throw new InvalidOperationException("Aidat kaydı bulunamadı.");

        dues.Amount = newAmount;
        if (dues.PaidAmount > dues.Amount) dues.PaidAmount = dues.Amount; // fazla tahsilatı törpüle
        dues.RecalculateStatus();

        await _db.SaveChangesAsync(ct);
        return Map(dues);
    }

    private static DuesDto Map(Dues d) => new()
    {
        Id = d.Id,
        ApartmentId = d.ApartmentId,
        Year = d.Year,
        Month = d.Month,
        Amount = d.Amount,
        PaidAmount = d.PaidAmount,
        Status = d.Status,
        PaidDate = d.PaidDate,
        ReceiptUrl = d.ReceiptUrl,
    };
}
