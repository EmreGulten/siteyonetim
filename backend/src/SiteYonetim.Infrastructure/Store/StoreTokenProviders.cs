using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace SiteYonetim.Infrastructure.Store;

/// <summary>
/// Apple App Store Server API için imzalı JWT üretir (ES256).
/// App Store Server API her istekte bu JWT'yi Bearer olarak ister.
/// Anahtar/ayarlar: APPLE_BUNDLE_ID, APPLE_KEY_ID, APPLE_ISSUER_ID, APPLE_PRIVATE_KEY (P8).
/// </summary>
public class AppleJwtProvider : IAppleJwtProvider
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<AppleJwtProvider> _logger;

    public AppleJwtProvider(IConfiguration cfg, ILogger<AppleJwtProvider> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    public Task<string> GetSignedJwtAsync(CancellationToken ct = default)
    {
        try
        {
            var bundleId = _cfg["APPLE_BUNDLE_ID"];
            var keyId = _cfg["APPLE_KEY_ID"];
            var issuerId = _cfg["APPLE_ISSUER_ID"];
            var privateKey = _cfg["APPLE_PRIVATE_KEY"];

            if (string.IsNullOrEmpty(bundleId) || string.IsNullOrEmpty(keyId)
                || string.IsNullOrEmpty(issuerId) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogWarning("Apple App Store API anahtarları eksik (APPLE_*).");
                return Task.FromResult(string.Empty);
            }

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(privateKey);

            var now = DateTime.UtcNow;
            var header = new JwtHeader(new SigningCredentials(
                new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256))
            {
                ["kid"] = keyId,
                ["typ"] = "JWT",
            };
            var payload = new JwtPayload
            {
                { "iss", issuerId },
                { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "exp", new DateTimeOffset(now.AddMinutes(20)).ToUnixTimeSeconds() },
                { "aud", "https://apple.com" },
                { "bid", bundleId },
            };
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
            return Task.FromResult(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple JWT üretilemedi.");
            return Task.FromResult(string.Empty);
        }
    }
}
