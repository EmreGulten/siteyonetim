using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Financial;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Dashboard finansal özet servisi. Hesaplamaları SQL aggregate fonksiyonlarıyla
/// (Sum) veritabanında yapar — client'a hazır değer döner.
/// </summary>
public class FinancialSummaryService
{
    private readonly IAppDbContext _db;

    public FinancialSummaryService(IAppDbContext db) => _db = db;

    public async Task<FinancialSummaryDto> GetAsync(int year, int month, CancellationToken ct = default)
    {
        // Beklenen gelir + tahsil edilen (aidatlar)
        var duesAgg = await _db.Dues
            .Where(d => d.Year == year && d.Month == month)
            .GroupBy(d => 1)
            .Select(g => new
            {
                Expected = g.Sum(d => d.Amount),
                Collected = g.Sum(d => d.PaidAmount),
            })
            .FirstOrDefaultAsync(ct) ?? new { Expected = 0m, Collected = 0m };

        // Gelir/Gider (dönemde)
        var txAgg = await _db.Transactions
            .Where(t => t.Date.Year == year && t.Date.Month == month)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        decimal expenses = txAgg.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Total);
        decimal otherIncome = txAgg.Where(x => x.Type == TransactionType.Income).Sum(x => x.Total);

        decimal netBalance = duesAgg.Collected + otherIncome - expenses;

        return new FinancialSummaryDto
        {
            ExpectedIncome = duesAgg.Expected,
            Collected = duesAgg.Collected,
            Outstanding = Math.Max(duesAgg.Expected - duesAgg.Collected, 0m),
            Expenses = expenses,
            OtherIncome = otherIncome,
            NetBalance = netBalance,
            CollectionRate = duesAgg.Expected == 0 ? 0 : Math.Round(duesAgg.Collected / duesAgg.Expected * 100m, 1),
            Year = year,
            Month = month,
        };
    }
}
