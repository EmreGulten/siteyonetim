using SiteYonetim.Application.DTOs.Auth;

namespace SiteYonetim.Application.Abstractions;

/// <summary>JWT token üretimi. SiteId + Role claim'lerini token'a gömer.</summary>
public interface ITokenService
{
    TokenResult GenerateAccessToken(Guid userId, string email, Guid? siteId, Domain.Enums.UserRole role);
    string GenerateRefreshToken();
}
