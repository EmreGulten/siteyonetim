using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Auth;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Infrastructure.Identity;

/// <summary>
/// JWT üretim servisi. Token içine UserId, Email, SiteId ve Role claim olarak gömülür.
/// Mobil taraf bu claim'leri okuyarak multi-tenancy kontekstini kurar.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public TokenResult GenerateAccessToken(Guid userId, string email, Guid? siteId, UserRole role)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(_opt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role.ToString()),
            // Özel claim'ler — mobil tenant konteksti
            new("site_id", siteId?.ToString() ?? string.Empty),
            new("role", role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new TokenResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = expiry,
        };
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
