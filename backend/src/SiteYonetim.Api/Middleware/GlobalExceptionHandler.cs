using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SiteYonetim.Api.Middleware;

/// <summary>
/// Merkezî istisna yönetimi. Tüm controller'lar tutarlı JSON hata yanıtı döner.
/// Üretimde yığın izi sızdırılmaz.
/// </summary>
public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        try { await next(ctx); }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Yetkisiz erişim: {Path}", ctx.Request.Path);
            await Write(ctx, HttpStatusCode.Unauthorized, "Yetkisiz", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await Write(ctx, HttpStatusCode.BadRequest, "Geçersiz işlem", ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            await Write(ctx, HttpStatusCode.BadRequest, "Geçersiz istek", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await Write(ctx, HttpStatusCode.BadRequest, "Geçersiz istek", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Write(ctx, HttpStatusCode.Conflict, "Çakışma", "Kayıt başka bir işlem tarafından değiştirildi (iyimser eşzamanlılık).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmemiş hata: {Path}", ctx.Request.Path);
            await Write(ctx, HttpStatusCode.InternalServerError, "Sunucu hatası",
                _env.IsDevelopment() ? ex.ToString() : "Beklenmeyen bir hata oluştu.");
        }
    }

    private static Task Write(HttpContext ctx, HttpStatusCode code, string title, string detail)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)code;
        var problem = new ProblemDetails { Status = (int)code, Title = title, Detail = detail, Type = $"https://httpstatuses.io/{(int)code}" };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
