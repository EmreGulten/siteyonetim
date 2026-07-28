using Hangfire;
using Microsoft.Extensions.Logging;
using SiteYonetim.Application.DTOs.Dues;
using SiteYonetim.Application.Services;

namespace SiteYonetim.Infrastructure.Jobs;

/// <summary>
/// Hangfire ile çalışan aidat üretim job'u. Her ayın 1'inde otomatik tetiklenir
/// (Program.cs'te RecurringJob kaydı). SiteManager tüm siteler için üretir
/// (global filter zaten tenant izolasyonu sağlar; SuperAdmin tüm siteleri üretir).
/// </summary>
public class DuesGenerationJob
{
    private readonly DuesGenerationService _service;
    private readonly ILogger<DuesGenerationJob> _logger;

    public DuesGenerationJob(DuesGenerationService service, ILogger<DuesGenerationJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Hangfire giriş noktası. Mevcut ay için üretir.</summary>
    public async Task RunMonthlyAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("Aidat üretim job'u başladı: {Year}-{Month:00}", now.Year, now.Month);
        var result = await _service.GenerateAsync(new GenerateDuesRequest { Year = now.Year, Month = now.Month });
        _logger.LogInformation("Aidat üretim tamamlandı: {@Result}", result);
    }
}
