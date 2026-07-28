namespace SiteYonetim.Infrastructure.Store;

/// <summary>
/// RevenueCat ayarları. .NET hiyerarşik config: env <c>RevenueCat__Secret</c>,
/// <c>RevenueCat__EntitlementId</c>. Secret yalnızca sunucuda (deploy/.env) tutulur,
/// mobil tarafa veya repo'ya ASLA sızdırılmaz.
/// </summary>
public sealed class RevenueCatOptions
{
    public const string SectionName = "RevenueCat";

    /// <summary>RevenueCat **secret** API anahtarı (sk_...). Yalnızca sunucu.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>RevenueCat'te oluşturulan entitlement kimliği (varsayılan: "premium").</summary>
    public string EntitlementId { get; set; } = "premium";
}
