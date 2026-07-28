using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Api.Middleware;

/// <summary>
/// Hangfire dashboard erişim filtresi: yalnızca SuperAdmin rolü erişebilir.
/// (Üretimde ek IP/VPN kısıtı önerilir.)
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var role = http.User?.FindFirst("role")?.Value;
        return string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
    }
}
