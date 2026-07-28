using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Subscription;

namespace SiteYonetim.Infrastructure.Store;

/// <summary>
/// Store fiş (receipt) doğrulayıcı. Tek başına mobil tarafı hacklenebilir olduğu için
/// doğrulama sunucuda, gerçek store API'lerine karşı yapılır:
///  - Google Play Developer API: purchases.subscriptions.get
///  - Apple App Store Server API (JWT imzalı)
/// </summary>
public class StoreReceiptVerifier : IStoreReceiptVerifier
{
    private readonly HttpClient _google;
    private readonly HttpClient _apple;
    private readonly IGoogleAccessTokenProvider _googleToken;
    private readonly IAppleJwtProvider _appleJwt;
    private readonly ILogger<StoreReceiptVerifier> _logger;

    public StoreReceiptVerifier(IHttpClientFactory http, IGoogleAccessTokenProvider googleToken,
        IAppleJwtProvider appleJwt, ILogger<StoreReceiptVerifier> logger)
    {
        _google = http.CreateClient("GooglePlay");
        _apple = http.CreateClient("AppleStore");
        _googleToken = googleToken;
        _appleJwt = appleJwt;
        _logger = logger;
    }

    public async Task<ReceiptVerificationResult> VerifyAsync(string store, string receiptPayload, string productId, CancellationToken ct = default)
    {
        try
        {
            return store switch
            {
                "GooglePlay" => await VerifyGoogleAsync(receiptPayload, productId, ct),
                "Apple" => await VerifyAppleAsync(receiptPayload, productId, ct),
                _ => Invalid("Bilinmeyen store"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Store doğrulaması başarısız: {Store}", store);
            return new ReceiptVerificationResult { IsValid = false, ErrorCode = "VERIFY_EXCEPTION" };
        }
    }

    private async Task<ReceiptVerificationResult> VerifyGoogleAsync(string purchaseToken, string productId, CancellationToken ct)
    {
        // Service account ile alınan OAuth access token (GoogleJwtAccessTokenProvider üretir).
        var accessToken = await _googleToken.GetAccessTokenAsync(ct);
        _google.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var packageName = _google.BaseAddress?.ToString().TrimEnd('/') ?? "";
        // Google Play Developer API v3
        var resp = await _google.GetAsync(
            $"androidpublisher/v3/applications/{packageName}/purchases/subscriptions/{productId}/tokens/{purchaseToken}", ct);

        if (!resp.IsSuccessStatusCode)
            return new ReceiptVerificationResult { IsValid = false, ErrorCode = $"GOOGLE_{resp.StatusCode}" };

        var json = await resp.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var root = json?.RootElement ?? default;

        // paymentState: 1 = Ödeme alındı, 0/2 = bekliyor/iade
        var paymentState = root.TryGetProperty("paymentState", out var ps) ? ps.GetInt32() : 1;
        var valid = paymentState == 1;
        var expiryMsec = root.TryGetProperty("expiryTimeMillis", out var e) ? e.GetInt64() : 0;
        var expiry = expiryMsec > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(expiryMsec).UtcDateTime : (DateTime?)null;

        return new ReceiptVerificationResult
        {
            IsValid = valid,
            IsFraudulent = false,
            ProductId = productId,
            ExpiryDate = expiry,
            StoreSubscriptionId = purchaseToken,
        };
    }

    private async Task<ReceiptVerificationResult> VerifyAppleAsync(string transactionId, string productId, CancellationToken ct)
    {
        // Apple App Store Server API (JWT imzalı)
        var jwt = await _appleJwt.GetSignedJwtAsync(ct);
        _apple.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var resp = await _apple.GetAsync($"inApps/v1/transactions/{transactionId}", ct);
        if (!resp.IsSuccessStatusCode)
            return new ReceiptVerificationResult { IsValid = false, ErrorCode = $"APPLE_{resp.StatusCode}" };

        // Apple yanıtında signedTransactions içinde expiryDate (ms)
        var body = await resp.Content.ReadAsStringAsync(ct);
        return new ReceiptVerificationResult
        {
            IsValid = body.Contains("\"status\":0") || body.Contains(productId),
            ProductId = productId,
            StoreSubscriptionId = transactionId,
            ExpiryDate = DateTimeOffset.FromUnixTimeMilliseconds(
                ExtractLong(body, "expiryDate")).UtcDateTime,
        };
    }

    private static long ExtractLong(string json, string key)
    {
        var idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return 0;
        var colon = json.IndexOf(':', idx);
        var end = json.IndexOfAny(new[] { ',', '}', ']' }, colon);
        return long.TryParse(json.AsSpan(colon + 1, end - colon - 1).Trim(), out var v) ? v : 0;
    }

    private static ReceiptVerificationResult Invalid(string code) =>
        new() { IsValid = false, ErrorCode = code };
}

/// <summary>Google OAuth access token sağlayıcı (service account ile).</summary>
public interface IGoogleAccessTokenProvider { Task<string> GetAccessTokenAsync(CancellationToken ct); }
/// <summary>Apple App Store Server API için imzalı JWT sağlayıcı.</summary>
public interface IAppleJwtProvider { Task<string> GetSignedJwtAsync(CancellationToken ct); }
