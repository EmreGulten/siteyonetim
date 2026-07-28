using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Apartments;
using SiteYonetim.Application.DTOs.Common;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Daire/blok/tip yönetimi. Listeleme (blok filtresi), ekleme, aylık aidat grafiği.
/// </summary>
public class ApartmentService
{
    private readonly IAppDbContext _db;
    private readonly PremiumPolicy _policy;

    public ApartmentService(IAppDbContext db, PremiumPolicy policy)
    {
        _db = db;
        _policy = policy;
    }

    public async Task<PagedResult<ApartmentDto>> ListAsync(int page, int pageSize, Guid? blockId, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = from a in _db.Apartments
                join b in _db.Blocks on a.BlockId equals b.Id
                // Tip artık opsiyonel → left join (tipsiz daireler de listelenir)
                join t in _db.ApartmentTypes on a.ApartmentTypeId equals t.Id into tg
                from t in tg.DefaultIfEmpty()
                where blockId == null || a.BlockId == blockId
                let resident = _db.Residents.Where(r => r.ApartmentId == a.Id && r.IsActive)
                                  .OrderByDescending(r => r.IsOwner).FirstOrDefault()
                orderby b.DisplayOrder, a.Floor, a.DoorNumber
                select new ApartmentDto
                {
                    Id = a.Id,
                    BlockId = a.BlockId,
                    BlockName = b.Name,
                    ApartmentTypeId = a.ApartmentTypeId,
                    ApartmentTypeName = t != null ? t.Name : null,
                    MonthlyDues = a.MonthlyDues,
                    DoorNumber = a.DoorNumber,
                    Floor = a.Floor,
                    IsOccupied = a.IsOccupied,
                    OwnerName = resident != null ? resident.FullName : null,
                    ResidentName = resident != null ? resident.FullName : null,
                    Phone = resident != null ? resident.Phone : null,
                };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(ct);

        return new PagedResult<ApartmentDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<List<BlockDto>> GetBlocksAsync(CancellationToken ct = default) =>
        await _db.Blocks.OrderBy(b => b.DisplayOrder).AsNoTracking()
            .Select(b => new BlockDto { Id = b.Id, Name = b.Name, DisplayOrder = b.DisplayOrder }).ToListAsync(ct);

    public async Task<List<ApartmentTypeDto>> GetTypesAsync(CancellationToken ct = default) =>
        await _db.ApartmentTypes.AsNoTracking()
            .Select(t => new ApartmentTypeDto { Id = t.Id, Name = t.Name, BaseDues = t.BaseDues, ArsaPayi = t.ArsaPayi }).ToListAsync(ct);

    public async Task<ApartmentDto> CreateAsync(CreateApartmentRequest req, CancellationToken ct = default)
    {
        // Free plan sınırı: en fazla 20 daire
        await _policy.EnsureCanAddApartmentAsync(ct);

        var apt = new Apartment
        {
            BlockId = req.BlockId,
            ApartmentTypeId = req.ApartmentTypeId,   // opsiyonel (artık tip zorunlu değil)
            MonthlyDues = req.MonthlyDues,
            DoorNumber = req.DoorNumber,
            Floor = req.Floor,
        };
        _db.Apartments.Add(apt);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(req.OwnerFullName))
        {
            _db.Residents.Add(new Resident
            {
                ApartmentId = apt.Id,
                SiteId = apt.SiteId,
                FullName = req.OwnerFullName,
                Phone = req.OwnerPhone,
                TcNo = req.OwnerTc,
                IsOwner = true,
                IsActive = true,
            });
            await _db.SaveChangesAsync(ct);
        }

        return new ApartmentDto
        {
            Id = apt.Id,
            BlockId = req.BlockId,
            DoorNumber = req.DoorNumber,
            Floor = req.Floor,
            ApartmentTypeId = req.ApartmentTypeId,
            MonthlyDues = apt.MonthlyDues,
            IsOccupied = true,
            OwnerName = req.OwnerFullName,
        };
    }

    /// <summary>Daireyi soft-delete eder (aidat/sakin kayıtları FK nedeniyle korunur, IsDeleted=true).</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var apt = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new InvalidOperationException("Daire bulunamadı.");
        _db.Apartments.Remove(apt); // OnBeforeSave soft-delete'e çevirir
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BlockDto> CreateBlockAsync(CreateBlockRequest req, CancellationToken ct = default)
    {
        var block = new Block { Name = req.Name, DisplayOrder = req.DisplayOrder };
        _db.Blocks.Add(block);
        await _db.SaveChangesAsync(ct);
        return new BlockDto { Id = block.Id, Name = block.Name, DisplayOrder = block.DisplayOrder };
    }

    /// <summary>Bloktaki (silinmemiş) daire sayısı — silme öncesi bütünlük kontrolü.</summary>
    public async Task<int> GetBlockApartmentCountAsync(Guid blockId, CancellationToken ct = default)
        => await _db.Apartments.CountAsync(a => a.BlockId == blockId, ct);

    /// <summary>Bloğu soft-delete eder (içinde daire yoksa çağrılmalı).</summary>
    public async Task DeleteBlockAsync(Guid blockId, CancellationToken ct = default)
    {
        var block = await _db.Blocks.FirstOrDefaultAsync(b => b.Id == blockId, ct)
            ?? throw new InvalidOperationException("Blok bulunamadı.");
        _db.Blocks.Remove(block); // OnBeforeSave soft-delete'e çevirir
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ApartmentTypeDto> CreateTypeAsync(CreateApartmentTypeRequest req, CancellationToken ct = default)
    {
        var type = new ApartmentType { Name = req.Name, BaseDues = req.BaseDues, ArsaPayi = req.ArsaPayi };
        _db.ApartmentTypes.Add(type);
        await _db.SaveChangesAsync(ct);
        return new ApartmentTypeDto { Id = type.Id, Name = type.Name, BaseDues = type.BaseDues, ArsaPayi = type.ArsaPayi };
    }

    /// <summary>Belirli dairenin aylık aidat grafiği verisi (son 12 ay).</summary>
    public async Task<List<MonthlyDuesPoint>> GetMonthlyChartAsync(Guid apartmentId, CancellationToken ct = default)
    {
        var cutoff = new DateTime(DateTime.UtcNow.AddYears(-1).Year, DateTime.UtcNow.AddYears(-1).Month, 1);
        return await _db.Dues
            .Where(d => d.ApartmentId == apartmentId
                        && (d.Year > cutoff.Year || (d.Year == cutoff.Year && d.Month >= cutoff.Month)))
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .AsNoTracking()
            .Select(d => new MonthlyDuesPoint { Year = d.Year, Month = d.Month, Amount = d.Amount, Paid = d.PaidAmount })
            .ToListAsync(ct);
    }
}
