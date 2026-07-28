namespace SiteYonetim.Application.DTOs.Subscription;

public enum Store { GooglePlay, Apple }

public sealed class VerifySubscriptionRequest
{
    public Store Store { get; set; }
    public string ReceiptPayload { get; set; } = string.Empty; // purchase token / receipt
    public string ProductId { get; set; } = string.Empty;
}

public sealed class ReceiptVerificationResult
{
    public bool IsValid { get; set; }
    public bool IsFraudulent { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string? StoreSubscriptionId { get; set; }
    public string? ErrorCode { get; set; }
}

public sealed class SubscriptionStatusDto
{
    public bool IsPremium { get; set; }
    public DateTime? PremiumExpiryDate { get; set; }
    public string Plan { get; set; } = "Free";
}

/// <summary>Manuel premium aktifleştirme (yalnızca SuperAdmin, test/geliştirme).</summary>
public sealed class GrantRequest
{
    public string ProductId { get; set; } = "premium.monthly";
}
