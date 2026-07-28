using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Common;
using SiteYonetim.Application.DTOs.Dues;
using SiteYonetim.Application.Services;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dues")]
public class DuesController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly CollectionService _collection;
    private readonly DuesGenerationService _generation;

    public DuesController(IAppDbContext db, CollectionService collection, DuesGenerationService generation)
    {
        _db = db;
        _collection = collection;
        _generation = generation;
    }

    /// <summary>Aidat listesi (yıl/ay opsiyonel filtre, sayfalı). Misafir erişebilir (boş döner).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<DuesDto>>> List(
        [FromQuery] int year, [FromQuery] int? month, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        IQueryable<Dues> filtered = _db.Dues;
        if (year > 0) filtered = filtered.Where(d => d.Year == year);
        if (month is > 0) filtered = filtered.Where(d => d.Month == month);

        var q = from d in filtered
                join a in _db.Apartments on d.ApartmentId equals a.Id
                join b in _db.Blocks on a.BlockId equals b.Id
                orderby b.DisplayOrder, a.Floor, a.DoorNumber
                select new DuesDto
                {
                    Id = d.Id, ApartmentId = d.ApartmentId, Year = d.Year, Month = d.Month,
                    ApartmentLabel = $"{b.Name} / Daire {a.DoorNumber}",
                    Amount = d.Amount, PaidAmount = d.PaidAmount, Status = d.Status,
                    PaidDate = d.PaidDate, ReceiptUrl = d.ReceiptUrl,
                };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(ct);

        return Ok(new PagedResult<DuesDto> { Items = items, Total = total, Page = page, PageSize = pageSize });
    }

    /// <summary>Hızlı tahsilat: tutarı gir, ödendi yap, (opsiyonel) PDF makbuz üret.</summary>
    [HttpPost("collect")]
    [Authorize(Policy = "SiteManager")]
    public async Task<ActionResult<CollectDuesResponse>> Collect([FromBody] CollectDuesRequest req, CancellationToken ct)
        => Ok(await _collection.CollectAsync(req, ct));

    /// <summary>Tek dairenin aidat tutarını güncelle (manuel override, SiteManager).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SiteManager")]
    public async Task<ActionResult<DuesDto>> UpdateAmount(Guid id, [FromBody] UpdateDuesRequest req, CancellationToken ct)
        => Ok(await _collection.UpdateAmountAsync(id, req.Amount, ct));

    /// <summary>Manuel aidat üretimi (ayın 1'i Hangfire job'unu elle tetikler).</summary>
    [HttpPost("generate")]
    [Authorize(Policy = "SiteManager")]
    public async Task<ActionResult<GenerateDuesResult>> Generate([FromBody] GenerateDuesRequest req, CancellationToken ct)
        => Ok(await _generation.GenerateAsync(req, ct));
}
