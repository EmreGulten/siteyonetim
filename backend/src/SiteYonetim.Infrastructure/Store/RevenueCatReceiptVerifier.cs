using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Subscription;

namespace SiteYonetim.Infrastructure.Store;

/// <summary>
/// RevenueCat REST ile abonelik doğrulama. Mobil taraf RevenueCat SDK üzerinden
/// satın alır (StoreKit/makbuz doğrulamasını RevenueCat yapar); satın alma sonrası
/// backend <c>/api/subscription/verify</c>'ı RevenueCat <c>appUserId</c> (= backend
/// User.Id) ile çağırır. Burada <c>GET /v1/subscribers/{appUserId}</c> (secret ile)
/// sorgulanır ve ilgili entitlement aktif mi (bitiş tarihi gelecekte mi) kontrol edilir.
/// Apple private key / JWT derdi yoktur.
/// </summary>
public class RevenueCatReceiptVerifier : IStoreReceiptVerifier
{
    private readonly HttpClient _http;
    private readonly RevenueCatOptions _opt;
    private readonly ILogger<RevenueCatReceiptVerifier> _logger;

    public RevenueCatReceiptVerifier(IHttpClientFactory http, IOptions<RevenueCatOptions> opt,
        ILogger<RevenueCatReceiptVerifier> logger)
    {
        _http = http.CreateClient("RevenueCat");
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<ReceiptVerificationResult> VerifyAsync(
        string store, string receiptPayload, string productId, CancellationToken ct = default)
    {
        // receiptPayload = RevenueCat appUserId (mobil, login sonrası User.Id'yi set eder).
        var appUserId = receiptPayload?.Trim();

        // Fail-closed: secret yoksa hiçbir satın almayı geçerli sayma.
        if (string.IsNullOrWhiteSpace(_opt.Secret))
        {
            _logger.LogError("RevenueCat Secret yapılandırılmamış (RevenueCat__Secret env eksik). Fail-closed.");
            return Invalid("NO_SECRET_CONFIG");
        }
        if (string.IsNullOrWhiteSpace(appUserId))
            return Invalid("NO_APP_USER_ID");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"v1/subscribers/{Uri.EscapeDataString(appUserId)}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.Secret);
            req.Headers.Add("X-Platform", "ios");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("RevenueCat subscriber sorgusu başarısız: appUserId={AppUserId} status={Status}",
                    appUserId, resp.StatusCode);
                return Invalid($"RC_{resp.StatusCode}");
            }

            var root = (await resp.Content.ReadFromJsonAsync<JsonDocument>(ct))?.RootElement ?? default;
            if (!root.TryGetProperty("subscriber", out var subscriber))
                return Invalid("NO_SUBSCRIBER");

            // subscriber.entitlements.{entitlementId}
            if (!subscriber.TryGetProperty("entitlements", out var entitlements) ||
                !entitlements.TryGetProperty(_opt.EntitlementId, out var ent))
            {
                // Entitlement yok → kullanıcı premium değil (geçerli ama aktif değil).
                return new ReceiptVerificationResult { IsValid = false, ErrorCode = "NO_ENTITLEMENT" };
            }

            var entProduct = ent.TryGetProperty("product_identifier", out var pid) ? pid.GetString() : productId;
            var expiry = ParseExpiry(ent);

            return new ReceiptVerificationResult
            {
                IsValid = expiry is { } exp && exp > DateTime.UtcNow,
                IsFraudulent = false,
                ProductId = entProduct ?? productId,
                ExpiryDate = expiry,
                StoreSubscriptionId = appUserId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RevenueCat doğrulaması istisnası: appUserId={AppUserId}", appUserId);
            return new ReceiptVerificationResult { IsValid = false, ErrorCode = "VERIFY_EXCEPTION" };
        }
    }

    /// <summary>RevenueCat REST alan adı sürümler arası farklı olabilir; ikisini de dene.</summary>
    private static DateTime? ParseExpiry(JsonElement ent)
    {
        foreach (var key in new[] { "expiration_date", "expires_date" })
        {
            if (ent.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(v.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dt))
            {
                return dt.ToUniversalTime();
            }
        }
        return null;
    }

    private static ReceiptVerificationResult Invalid(string code) =>
        new() { IsValid = false, ErrorCode = code };
}
