using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Application.DTOs.Subscription;
using SiteYonetim.Application.Services;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _svc;
    public SubscriptionController(SubscriptionService svc) => _svc = svc;

    /// <summary>Mobil IAP receipt'ini güvenli şekilde doğrula (FAZ 5).</summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifySubscriptionRequest req, CancellationToken ct)
        => Ok(await _svc.VerifyAsync(req, ct));

    /// <summary>Mevcut kullanıcı premium durumu (uygulama açılışında çağrılır).</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(await _svc.GetStatusAsync(ct));

    /// <summary>Manuel premium aktifleştirme — yalnızca SuperAdmin (test/geliştirme).</summary>
    [HttpPost("grant")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Grant([FromBody] GrantRequest? req, CancellationToken ct)
        => Ok(await _svc.GrantAsync(req?.ProductId ?? "premium.monthly", ct));
}
