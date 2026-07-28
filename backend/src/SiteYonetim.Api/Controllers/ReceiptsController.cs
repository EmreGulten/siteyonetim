using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Application.Services;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/receipts")]
public class ReceiptsController : ControllerBase
{
    private readonly ReceiptService _receipts;
    public ReceiptsController(ReceiptService receipts) => _receipts = receipts;

    /// <summary>
    /// Verilen aidat için tahsilat makbuzu PDF üretir ve byte[] olarak döner.
    /// Mobil bu endpoint'i çağırıp PDF'i indirir. (Rehber: /api/receipts/generate/{duesId})
    /// </summary>
    [HttpGet("generate/{duesId:guid}")]
    [Authorize(Policy = "SiteManager")]
    public async Task<IActionResult> Generate(Guid duesId, CancellationToken ct)
    {
        var (pdf, _) = await _receipts.GenerateAsync(duesId, persist: true, ct);
        return File(pdf, "application/pdf", $"makbuz-{duesId:N}.pdf");
    }
}
