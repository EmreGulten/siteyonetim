using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Reports;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Rapor servisi. Borçlu daireler ve KMK uyumlu raporlar üretir. DTO olarak döner.
/// TC Kimlik No listelemede maskelenir (FAZ 6 güvenliği).
/// </summary>
public class ReportService
{
    private readonly IAppDbContext _db;
    private readonly IIhtarnamePdfRenderer _ihtarnamePdf;
    private readonly IBalancePdfRenderer _balancePdf;

    public ReportService(IAppDbContext db, IIhtarnamePdfRenderer ihtarnamePdf, IBalancePdfRenderer balancePdf)
    {
        _db = db;
        _ihtarnamePdf = ihtarnamePdf;
        _balancePdf = balancePdf;
    }

    /// <summary>Borçlu daireler listesi (kalan borç &gt; 0), gecikme ayı ile birlikte.</summary>
    public async Task<List<DebtorApartmentDto>> GetDebtorsAsync(ReportFilter filter, CancellationToken ct = default)
    {
        // 1) Borçlu daire başına toplam borç (group by) — çevirilebilir sorgu.
        //    Grup içinde alt sorgu (resident) EF Core tarafından çevrilemediği için
        //    sakinleri 2. sorguda ayrıca çekip client'ta birleştiriyoruz.
        var debts = await (
            from d in _db.Dues
            join a in _db.Apartments on d.ApartmentId equals a.Id
            join b in _db.Blocks on a.BlockId equals b.Id
            where d.Amount > d.PaidAmount
                  && d.Year == filter.Year
                  && (filter.Month == null || d.Month == filter.Month)
                  && (filter.BlockId == null || a.BlockId == filter.BlockId)
            group d by new { a.Id, a.DoorNumber, BlockName = b.Name } into g
            select new
            {
                ApartmentId = g.Key.Id,
                g.Key.DoorNumber,
                g.Key.BlockName,
                TotalDebt = g.Sum(d => d.Amount - d.PaidAmount),
                OverdueMonths = g.Count(),
            }
        ).AsNoTracking().ToListAsync(ct);

        if (debts.Count == 0) return new List<DebtorApartmentDto>();

        // 2) Bu dairelerin aktif sakinleri; malik (IsOwner) öncelikli — client'ta seç.
        var apartmentIds = debts.Select(x => x.ApartmentId).ToList();
        var residents = await _db.Residents
            .Where(r => r.IsActive && apartmentIds.Contains(r.ApartmentId))
            .Select(r => new { r.ApartmentId, r.FullName, r.Phone, r.TcNo, r.IsOwner })
            .AsNoTracking().ToListAsync(ct);
        var residentByApt = residents
            .GroupBy(r => r.ApartmentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.IsOwner).First());

        return debts
            .OrderByDescending(x => x.TotalDebt)
            .Select(x =>
            {
                residentByApt.TryGetValue(x.ApartmentId, out var r);
                return new DebtorApartmentDto
                {
                    ApartmentId = x.ApartmentId,
                    ApartmentLabel = $"{x.BlockName} / Daire {x.DoorNumber}",
                    TotalDebt = x.TotalDebt,
                    OverdueMonths = x.OverdueMonths,
                    ResidentName = r?.FullName,
                    OwnerName = r?.FullName,
                    Phone = r?.Phone,
                    TcMasked = MaskTc(r?.TcNo),
                };
            }).ToList();
    }

    /// <summary>
    /// KMK (Kat Mülkiyeti Kanunu) uyumlu "bildirim için hazır" daireler.
    /// Belirli eşiği aşan borcu olanlar bildirim almaya uygun işaretlenir.
    /// Premium özellik (FAZ 5).
    /// </summary>
    public async Task<List<KmkReadyDto>> GetKmkReportAsync(ReportFilter filter, CancellationToken ct = default)
    {
        // Grup/alt sorgu içeren tek sorgu EF Core'da çevrilemediği için 3 sorguya
        // bölüp client'ta birleştiriyoruz.
        var apartments = await (
            from a in _db.Apartments
            join b in _db.Blocks on a.BlockId equals b.Id
            select new { a.Id, BlockName = b.Name, a.DoorNumber }
        ).AsNoTracking().ToListAsync(ct);

        if (apartments.Count == 0) return new List<KmkReadyDto>();

        var aptIds = apartments.Select(x => x.Id).ToList();

        // Yıl aidat özeti (apartment başına).
        var duesAgg = await (
            from d in _db.Dues
            where d.Year == filter.Year && aptIds.Contains(d.ApartmentId)
            group d by d.ApartmentId into g
            select new
            {
                ApartmentId = g.Key,
                AnnualDues = g.Sum(d => d.Amount),
                Collected = g.Sum(d => d.PaidAmount),
                UnpaidMonths = g.Count(d => d.Amount > d.PaidAmount),
            }
        ).AsNoTracking().ToListAsync(ct);
        var duesByApt = duesAgg.ToDictionary(x => x.ApartmentId);

        // Malikler (her daireden bir).
        var owners = await (
            from r in _db.Residents
            where r.IsOwner && r.IsActive && aptIds.Contains(r.ApartmentId)
            select new { r.ApartmentId, r.FullName }
        ).AsNoTracking().ToListAsync(ct);
        var ownerByApt = owners.GroupBy(r => r.ApartmentId).ToDictionary(g => g.Key, g => g.First().FullName);

        return apartments.Select(a =>
        {
            duesByApt.TryGetValue(a.Id, out var d);
            var unpaidMonths = d?.UnpaidMonths ?? 0;
            var ready = unpaidMonths >= filter.OverdueThresholdMonths;
            return new KmkReadyDto
            {
                ApartmentId = a.Id,
                ApartmentLabel = $"{a.BlockName} / Daire {a.DoorNumber}",
                OwnerName = ownerByApt.GetValueOrDefault(a.Id),
                AnnualDues = d?.AnnualDues ?? 0m,
                CollectedThisYear = d?.Collected ?? 0m,
                IsKmkReady = ready,
                Note = ready ? "Bildirim için hazır" : "Borç eşiği altında",
            };
        }).ToList();
    }

    /// <summary>
    /// Aidat raporu: seçili yıla (ve opsiyonel aya/bloka) ait tüm aidat kalemleri.
    /// Daire bazında tutar / tahsil edilen / kalan borç gösterilir.
    /// </summary>
    public async Task<List<DuesReportRowDto>> GetDuesReportAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var query = from d in _db.Dues
                    join a in _db.Apartments on d.ApartmentId equals a.Id
                    join b in _db.Blocks on a.BlockId equals b.Id
                    where d.Year == filter.Year
                          && (filter.Month == null || d.Month == filter.Month)
                          && (filter.BlockId == null || a.BlockId == filter.BlockId)
                    orderby b.DisplayOrder, a.Floor, a.DoorNumber, d.Month
                    select new DuesReportRowDto
                    {
                        ApartmentId = a.Id,
                        ApartmentLabel = $"{b.Name} / Daire {a.DoorNumber}",
                        Year = d.Year,
                        Month = d.Month,
                        Amount = d.Amount,
                        PaidAmount = d.PaidAmount,
                        Status = d.Status,
                    };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Ek aidat raporu: aktif/geçmiş ek aidat kampanyaları + daire tipi farkları (Premium).
    /// Her satır bir kampanya-tip ikilisidir.
    /// </summary>
    public async Task<List<ExtraDuesReportRowDto>> GetExtraDuesReportAsync(CancellationToken ct = default)
    {
        var query = from e in _db.ExtraDues
                    from diff in _db.ExtraDuesDifferences.Where(x => x.ExtraDuesId == e.Id).DefaultIfEmpty()
                    join t in _db.ApartmentTypes on diff.ApartmentTypeId equals t.Id into tg
                    from t in tg.DefaultIfEmpty()
                    orderby e.IsActive descending, e.StartDate descending
                    select new ExtraDuesReportRowDto
                    {
                        ExtraDuesId = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        ApartmentTypeName = t != null ? t.Name : null,
                        Amount = diff != null ? diff.Amount : 0m,
                        InstallmentCount = e.InstallmentCount,
                        IsActive = e.IsActive,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                    };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Daire raporu: tüm bağımsız bölümler; blok, kat, malik, telefon ve aylık aidat.
    /// </summary>
    public async Task<List<ApartmentReportRowDto>> GetApartmentReportAsync(Guid? blockId = null, CancellationToken ct = default)
    {
        var query = from a in _db.Apartments
                    join b in _db.Blocks on a.BlockId equals b.Id
                    where blockId == null || a.BlockId == blockId
                    let resident = _db.Residents.Where(r => r.ApartmentId == a.Id && r.IsActive)
                                      .OrderByDescending(r => r.IsOwner).FirstOrDefault()
                    orderby b.DisplayOrder, a.Floor, a.DoorNumber
                    select new ApartmentReportRowDto
                    {
                        ApartmentId = a.Id,
                        ApartmentLabel = $"{b.Name} / Daire {a.DoorNumber}",
                        BlockName = b.Name,
                        DoorNumber = a.DoorNumber,
                        Floor = a.Floor,
                        MonthlyDues = a.MonthlyDues,
                        IsOccupied = a.IsOccupied,
                        OwnerName = resident != null ? resident.FullName : null,
                        Phone = resident != null ? resident.Phone : null,
                    };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Gelir/Gider/Detaylı işlem raporu: <paramref name="type"/> null ise hepsi,
    /// değilse yalnızca o tür. <paramref name="year"/> 0'dan büyükse o yılla süzülür.
    /// </summary>
    public async Task<List<TransactionReportRowDto>> GetTransactionsReportAsync(
        TransactionType? type = null, int year = 0, CancellationToken ct = default)
    {
        var query = from t in _db.Transactions
                    where (type == null || t.Type == type)
                          && (year <= 0 || t.Date.Year == year)
                    orderby t.Date descending
                    select new TransactionReportRowDto
                    {
                        Id = t.Id,
                        Type = t.Type,
                        Category = t.Category,
                        Description = t.Description,
                        Amount = t.Amount,
                        Date = t.Date,
                    };

        return await query.AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// KMK ihtarname verisini hazırlar: daire + malik + yıl içindeki ödenmemiş aidatlar.
    /// </summary>
    public async Task<IhtarnameData> GetIhtarnameDataAsync(Guid apartmentId, int year, CancellationToken ct = default)
    {
        var apt = await (from a in _db.Apartments
                         join b in _db.Blocks on a.BlockId equals b.Id
                         where a.Id == apartmentId
                         select new { Apartment = a, Block = b }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Daire bulunamadı.");

        var owner = await _db.Residents
            .Where(r => r.ApartmentId == apartmentId && r.IsActive)
            .OrderByDescending(r => r.IsOwner)
            .FirstOrDefaultAsync(ct);

        var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(ct);

        var dues = await _db.Dues
            .Where(d => d.ApartmentId == apartmentId && d.Year == year && d.Amount > d.PaidAmount)
            .OrderBy(d => d.Month)
            .AsNoTracking().ToListAsync(ct);

        return new IhtarnameData
        {
            SiteName = site?.Name ?? "Site Yönetimi",
            SiteAddress = site?.Address,
            ApartmentLabel = $"{apt.Block.Name} / Daire {apt.Apartment.DoorNumber}",
            OwnerName = owner?.FullName,
            OwnerTcMasked = MaskTc(owner?.TcNo),
            Phone = owner?.Phone,
            Year = year,
            Rows = dues.Select(d => new IhtarnameDuesRow { Year = d.Year, Month = d.Month, Remaining = d.RemainingAmount }).ToList(),
            TotalDebt = dues.Sum(d => d.RemainingAmount),
            ManagerName = site is null ? null : $"{site.Name} Yönetimi",
            IssuedAt = DateTime.UtcNow,
        };
    }

    /// <summary>İhtarname PDF'ini (byte[]) üretir. Premium özellik.</summary>
    public async Task<byte[]> GetIhtarnamePdfAsync(Guid apartmentId, int year, CancellationToken ct = default)
    {
        var data = await GetIhtarnameDataAsync(apartmentId, year, ct);
        return _ihtarnamePdf.Render(data);
    }

    /// <summary>Yıllık mali bilanço verisi: aylık gelir/gider/net (Premium).</summary>
    public async Task<AnnualBalanceData> GetAnnualBalanceDataAsync(int year, CancellationToken ct = default)
    {
        var txs = await _db.Transactions
            .Where(t => t.Date.Year == year)
            .Select(t => new { t.Type, t.Amount, t.Date.Month })
            .AsNoTracking().ToListAsync(ct);

        var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(ct);

        var rows = Enumerable.Range(1, 12).Select(m => new AnnualBalanceRow
        {
            Month = m,
            Income = txs.Where(t => t.Type == TransactionType.Income && t.Month == m).Sum(t => t.Amount),
            Expense = txs.Where(t => t.Type == TransactionType.Expense && t.Month == m).Sum(t => t.Amount),
        }).ToList();

        return new AnnualBalanceData
        {
            SiteName = site?.Name ?? "Site Yönetimi",
            Year = year,
            Rows = rows,
            TotalIncome = rows.Sum(r => r.Income),
            TotalExpense = rows.Sum(r => r.Expense),
            NetBalance = rows.Sum(r => r.Net),
            IssuedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Yıllık mali bilanço PDF'ini üretir. Premium özellik.</summary>
    public async Task<byte[]> GetAnnualBalancePdfAsync(int year, CancellationToken ct = default)
    {
        var data = await GetAnnualBalanceDataAsync(year, ct);
        return _balancePdf.Render(data);
    }

    /// <summary>Tüm site verisinin ZIP yedeğini üretir (JSON dosyaları). Premium özellik.</summary>
    public async Task<byte[]> GetBackupZipAsync(CancellationToken ct = default)
    {
        var apartments = await _db.Apartments.AsNoTracking().ToListAsync(ct);
        var dues = await _db.Dues.AsNoTracking().ToListAsync(ct);
        var transactions = await _db.Transactions.AsNoTracking().ToListAsync(ct);
        var residents = await _db.Residents.AsNoTracking().ToListAsync(ct);

        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            void AddJson(string name, object data)
            {
                var entry = zip.CreateEntry(name);
                using var ws = new StreamWriter(entry.Open());
                ws.Write(System.Text.Json.JsonSerializer.Serialize(data));
            }
            AddJson("apartments.json", apartments);
            AddJson("dues.json", dues);
            AddJson("transactions.json", transactions);
            AddJson("residents.json", residents);
        }
        return ms.ToArray();
    }

    /// <summary>TC Kimlik No'yu maskele: yalnızca son 4 hane görünür (örn. *******1234).</summary>
    public static string? MaskTc(string? tc)
    {
        if (string.IsNullOrWhiteSpace(tc)) return null;
        var digits = new string(tc.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return new string('*', tc.Length);
        return new string('*', digits.Length - 4) + digits[^4..];
    }
}
