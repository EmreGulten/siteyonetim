using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Dues;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Aidat üretim servisi. Ayın 1'inde Hangfire background job olarak (veya manuel
/// endpoint ile) çalışır. Akış:
///   1) Sadece o SiteId'ye ait daireleri çeker (global filter zaten filtreler).
///   2) Daire tipine göre temel aidatı alır.
///   3) Exemptions tablosuna bakar — muaf/de indirimli ise uygular.
///   4) O dönem aktif ExtraDues varsa, daire tipine göre farkını ekler.
///   5) Hesaplanan tutarı (Breakdown JSONB ile) Dues tablosuna kaydeder.
/// LINQ kullanılır — Raw SQL yok (SQL Injection önlemi, FAZ 6).
/// </summary>
public class DuesGenerationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ILogger<DuesGenerationService> _logger;

    public DuesGenerationService(IAppDbContext db, ICurrentUserService current, ILogger<DuesGenerationService> logger)
    {
        _db = db;
        _current = current;
        _logger = logger;
    }

    public async Task<GenerateDuesResult> GenerateAsync(GenerateDuesRequest req, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int year = req.Year == 0 ? now.Year : req.Year;
        int month = req.Month == 0 ? now.Month : req.Month;
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        // Aktif ek aidat farklarını bir kez çek (tip → tutar).
        var extraByType = await (
            from ed in _db.ExtraDues
            where ed.IsActive && ed.StartDate <= periodStart && ed.EndDate >= periodStart
            join diff in _db.ExtraDuesDifferences on ed.Id equals diff.ExtraDuesId
            select new { diff.ApartmentTypeId, diff.Amount }
        ).ToDictionaryAsync(x => x.ApartmentTypeId, x => x.Amount, ct);

        // Tüm daireler + tip + aktif muafiyet (tenant filter otomatik).
        var apartments = await _db.Apartments
            .Include(a => a.ApartmentType)
            .Include(a => a.Exemptions)
            .ToListAsync(ct);

        int created = 0, exempted = 0;
        var newDues = new List<Dues>();

        foreach (var apt in apartments)
        {
            // Aynı dönem için zaten kayıt varsa atla (idempotent).
            var exists = await _db.Dues.AnyAsync(d => d.ApartmentId == apt.Id && d.Year == year && d.Month == month, ct);
            if (exists) continue;

            // Aidat: öncelik dairenin kendi aylık aidatı; yoksa tipin BaseDues'una düşer.
            decimal baseDues = apt.MonthlyDues > 0
                ? apt.MonthlyDues
                : (apt.ApartmentType?.BaseDues ?? 0m);

            // Muafiyet kontrolü
            var exemption = apt.Exemptions.FirstOrDefault(e =>
                e.IsActive && e.StartDate <= periodStart && e.EndDate >= periodStart);

            decimal exemptionAmount = 0m;
            if (exemption is not null)
            {
                exemptionAmount = baseDues * exemption.DiscountRatio; // indirim tutarı
                exempted++;
            }

            // Ek aidat (tipe göre) — tip opsiyonel olduğundan null kontrolü
            decimal extraDues = (apt.ApartmentTypeId is { } tid && extraByType.TryGetValue(tid, out var x)) ? x : 0m;

            decimal total = baseDues + extraDues - exemptionAmount;

            var dues = new Dues
            {
                ApartmentId = apt.Id,
                SiteId = apt.SiteId,
                Year = year,
                Month = month,
                Amount = Math.Max(total, 0m),
                PaidAmount = 0m,
                Status = DuesStatus.Unpaid,
                DueDate = periodStart.AddDays(10), // örneğin ayın 10'u son ödeme
                Breakdown = new Dictionary<string, decimal>
                {
                    ["base"] = baseDues,
                    ["extra"] = extraDues,
                    ["exemption"] = -exemptionAmount,
                },
            };
            newDues.Add(dues);
            created++;
        }

        if (newDues.Count > 0)
        {
            await _db.Dues.AddRangeAsync(newDues, ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Aidat üretildi: Site={Site} {Year}-{Month:00} | İşlenen={Processed} Oluşturulan={Created} Muaf={Exempt}",
            _current.SiteId, year, month, apartments.Count, created, exempted);

        return new GenerateDuesResult
        {
            ApartmentsProcessed = apartments.Count,
            DuesCreated = created,
            Exempted = exempted,
        };
    }
}
