using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Financial;
using SiteYonetim.Application.DTOs.Common;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Gelir/gider servisi. Ekleme sırasında yüklenen fatura/makbuz görseli MinIO'ya
/// yüklenir; DB'ye sadece URL yazılır (FAZ 3 dosya yönetimi).
/// </summary>
public class TransactionService
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly PremiumPolicy _policy;

    public TransactionService(IAppDbContext db, IFileStorage storage, PremiumPolicy policy)
    {
        _db = db;
        _storage = storage;
        _policy = policy;
    }

    public async Task<PagedResult<TransactionDto>> ListAsync(TransactionType? type, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Transactions.AsNoTracking();
        if (type is not null) q = q.Where(t => t.Type == type.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.Date).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id, Type = t.Type, Category = t.Category, Description = t.Description,
                Amount = t.Amount, Date = t.Date, DocumentUrl = t.DocumentUrl,
            }).ToListAsync(ct);

        return new PagedResult<TransactionDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    /// <summary>İşlem ekle. documentStream verilirse MinIO'ya yüklenir.</summary>
    public async Task<TransactionDto> AddAsync(CreateTransactionRequest req, Stream? documentStream, string? documentFileName, CancellationToken ct = default)
    {
        string? url = null;
        if (documentStream is not null && !string.IsNullOrWhiteSpace(documentFileName))
        {
            // Free plan: ayda 5 belge limiti; Premium sınırsız.
            await _policy.EnsureCanUploadDocumentAsync(ct);
            var ext = Path.GetExtension(documentFileName);
            var contentType = ext.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream",
            };
            url = await _storage.UploadAsync(documentStream, $"docs/{Guid.NewGuid():N}{ext}", contentType, ct);
        }

        var tx = new FinancialTransaction
        {
            Type = req.Type,
            Category = req.Category,
            Description = req.Description,
            Amount = req.Amount,
            Date = req.Date,
            DocumentUrl = url,
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        return new TransactionDto
        {
            Id = tx.Id, Type = tx.Type, Category = tx.Category, Description = tx.Description,
            Amount = tx.Amount, Date = tx.Date, DocumentUrl = tx.DocumentUrl,
        };
    }
}
