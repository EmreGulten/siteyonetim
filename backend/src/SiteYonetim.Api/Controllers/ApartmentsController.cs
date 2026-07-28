using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Application.DTOs.Apartments;
using SiteYonetim.Application.Services;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/apartments")]
public class ApartmentsController : ControllerBase
{
    private readonly ApartmentService _svc;
    public ApartmentsController(ApartmentService svc) => _svc = svc;

    /// <summary>Daireleri listele (blok filtresi, sayfalı). Misafir erişebilir (boş döner).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? blockId = null, CancellationToken ct = default)
        => Ok(await _svc.ListAsync(page, pageSize, blockId, ct));

    [HttpGet("blocks")]
    [AllowAnonymous]
    public async Task<IActionResult> Blocks(CancellationToken ct) => Ok(await _svc.GetBlocksAsync(ct));

    [HttpGet("types")]
    [AllowAnonymous]
    public async Task<IActionResult> Types(CancellationToken ct) => Ok(await _svc.GetTypesAsync(ct));

    /// <summary>Dairenin aylık aidat grafiği. Misafir erişebilir.</summary>
    [HttpGet("{apartmentId:guid}/chart")]
    [AllowAnonymous]
    public async Task<IActionResult> Chart(Guid apartmentId, CancellationToken ct)
        => Ok(await _svc.GetMonthlyChartAsync(apartmentId, ct));

    [HttpPost]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Create([FromBody] CreateApartmentRequest req, CancellationToken ct)
        => Ok(await _svc.CreateAsync(req, ct));

    [HttpPost("blocks")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> CreateBlock([FromBody] CreateBlockRequest req, CancellationToken ct)
        => Ok(await _svc.CreateBlockAsync(req, ct));

    /// <summary>Blok sil — içinde daire varsa 409 ile reddeder (uyarı mesajı döner).</summary>
    [HttpDelete("blocks/{id:guid}")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> DeleteBlock(Guid id, CancellationToken ct)
    {
        var count = await _svc.GetBlockApartmentCountAsync(id, ct);
        if (count > 0)
            return Conflict(new { error = $"Bu blokta {count} daire bulunduğu için silinemez. Önce bu bloktaki daireleri silin." });
        await _svc.DeleteBlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("types")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> CreateType([FromBody] CreateApartmentTypeRequest req, CancellationToken ct)
        => Ok(await _svc.CreateTypeAsync(req, ct));

    /// <summary>Daire sil (soft-delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
