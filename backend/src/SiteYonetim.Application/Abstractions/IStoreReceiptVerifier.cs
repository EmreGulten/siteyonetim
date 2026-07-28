using SiteYonetim.Application.DTOs.Subscription;

namespace SiteYonetim.Application.Abstractions;

/// <summary>
/// Store (Google Play / Apple) fiş doğrulayıcı. FAZ 5'te Infrastructure implementasyonu
/// Google Play Developer API / Apple App Store Server API'ye bağlanır.
/// </summary>
public interface IStoreReceiptVerifier
{
    Task<ReceiptVerificationResult> VerifyAsync(string store, string receiptPayload, string productId, CancellationToken ct = default);
}
