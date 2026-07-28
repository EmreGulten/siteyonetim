using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Infrastructure.Identity;

/// <summary>
/// Mevcut kullanıcı servisi. JWT claim'lerinden (HttpContext) UserId/SiteId/Role okur.
/// AppDbContext ve Application servisleri bunu kullanır. Kimlik doğrulanmamışsa null'lar.
/// </summary>
public class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<HttpCurrentUserService> _logger;

    public HttpCurrentUserService(IHttpContextAccessor accessor, ILogger<HttpCurrentUserService> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ParseGuid(ClaimTypes.NameIdentifier);

    public Guid? SiteId => ParseGuid("site_id");

    public UserRole? Role => Enum.TryParse<UserRole>(_accessor.HttpContext?.User?.FindFirst("role")?.Value, out var r) ? r : null;

    private Guid? ParseGuid(string claimType)
    {
        var v = _accessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        return Guid.TryParse(v, out var g) ? g : null;
    }
}
