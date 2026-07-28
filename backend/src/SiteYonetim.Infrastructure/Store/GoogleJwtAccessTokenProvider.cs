using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace SiteYonetim.Infrastructure.Store;

/// <summary>
/// Google service-account ile OAuth access token üretir (RS256 imzalı JWT → token endpoint).
/// StoreReceiptVerifier, Google Play Developer API çağrısı için bunu kullanır.
/// Servis hesabı anahtarı: secrets/google-play-sa.json (env: GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_PATH).
/// Token önbelleğe alınır (~50 dk).
/// </summary>
public class GoogleJwtAccessTokenProvider : IGoogleAccessTokenProvider
{
    private readonly IConfiguration _cfg;
    private readonly HttpClient _http;
    private readonly ILogger<GoogleJwtAccessTokenProvider> _logger;

    private record SaJson(string client_email, string private_key, string token_uri);

    private static string? _cachedToken;
    private static DateTime _cacheExpiry;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public GoogleJwtAccessTokenProvider(IConfiguration cfg, IHttpClientFactory http,
        ILogger<GoogleJwtAccessTokenProvider> logger)
    {
        _cfg = cfg;
        _http = http.CreateClient("GoogleOAuth");
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && _cacheExpiry > DateTime.UtcNow.AddMinutes(2))
                return _cachedToken;

            var sa = LoadServiceAccount();
            if (sa is null) return string.Empty;

            var now = DateTime.UtcNow;
            var assertion = BuildAssertion(sa, now);

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            });

            var resp = await _http.PostAsync(sa.token_uri, form, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var token = json!.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = json.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;

            _cachedToken = token;
            _cacheExpiry = now.AddSeconds(expiresIn);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth access token alınamadı.");
            return string.Empty;
        }
        finally { _gate.Release(); }
    }

    private SaJson? LoadServiceAccount()
    {
        var path = _cfg["GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_PATH"];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _logger.LogWarning("Google service account JSON bulunamadı (GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_PATH).");
            return null;
        }
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SaJson>(json);
    }

    private static string BuildAssertion(SaJson sa, DateTime now)
    {
        using var rsa = RSA.Create();
        // PKCS#8 private key (service account'larda bu formatta gelir)
        rsa.ImportFromPem(sa.private_key);

        var signing = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var jwt = new JwtSecurityToken(
            issuer: sa.client_email,
            audience: sa.token_uri,
            claims: new[] { new System.Security.Claims.Claim("scope", "https://www.googleapis.com/auth/androidpublisher") },
            notBefore: now, expires: now.AddHours(1), signingCredentials: signing);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
