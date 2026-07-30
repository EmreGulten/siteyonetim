using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SiteYonetim.Application.DTOs.Financial;
using SiteYonetim.Application.Services;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("per-ip")]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly FinancialSummaryService _summary;
    private readonly TransactionService _tx;

    public FinanceController(FinancialSummaryService summary, TransactionService tx)
    {
        _summary = summary;
        _tx = tx;
    }

    /// <summary>Dashboard finansal özet (SQL aggregate). Misafir erişebilir (boş döner).</summary>
    [HttpGet("summary/{year}/{month}")]
    [AllowAnonymous]
    public async Task<IActionResult> Summary(int year, int month, CancellationToken ct)
        => Ok(await _summary.GetAsync(year, month, ct));

    [HttpGet("transactions")]
    [AllowAnonymous]
    public async Task<IActionResult> Transactions([FromQuery] TransactionType? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _tx.ListAsync(type, page, pageSize, ct));

    /// <summary>Gelir/gider ekle (opsiyonel fatura görseli multipart ile).</summary>
    [HttpPost("transactions")]
    [Authorize(Policy = "SiteManager")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Add([FromForm] CreateTransactionRequest req, IFormFile? document, CancellationToken ct)
    {
        Stream? stream = null; string? name = null;
        if (document is { Length: > 0 })
        {
            stream = document.OpenReadStream();
            name = document.FileName;
        }
        return Ok(await _tx.AddAsync(req, stream, name, ct));
    }
}
