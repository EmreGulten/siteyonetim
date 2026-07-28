using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Subscription;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Premium abonelik servisi (FAZ 5). Mobil IAP receipt'ini backend üzerinden doğrular:
/// tek başına mobil tarafı hacklenebilir olduğu için doğrulama sunucuda yapılır.
/// Doğrulanırsa User.Plan/PremiumExpiryDate güncellenir.
/// </summary>
public class SubscriptionService
{
    private readonly IAppDbContext _db;
    private readonly IStoreReceiptVerifier _verifier;
    private readonly ICurrentUserService _current;

    public SubscriptionService(IAppDbContext db, IStoreReceiptVerifier verifier, ICurrentUserService current)
    {
        _db = db;
        _verifier = verifier;
        _current = current;
    }

    public async Task<SubscriptionStatusDto> VerifyAsync(VerifySubscriptionRequest req, CancellationToken ct = default)
    {
        var userId = _current.UserId ?? throw new UnauthorizedAccessException();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        // Güvenli doğrulama: fiş gerçekten ödendi mi? Dolandırıcılık değil mi?
        var result = await _verifier.VerifyAsync(req.Store.ToString(), req.ReceiptPayload, req.ProductId, ct);

        if (!result.IsValid || result.IsFraudulent)
        {
            return new SubscriptionStatusDto
            {
                IsPremium = user.IsPremium,
                PremiumExpiryDate = user.PremiumExpiryDate,
                Plan = user.Plan.ToString(),
            };
        }

        user.Plan = SubscriptionPlan.Premium;
        user.PremiumExpiryDate = result.ExpiryDate ?? DateTime.UtcNow.AddYears(1);
        user.StoreSubscriptionId = result.StoreSubscriptionId ?? req.ReceiptPayload;
        await _db.SaveChangesAsync(ct);

        return new SubscriptionStatusDto
        {
            IsPremium = user.IsPremium,
            PremiumExpiryDate = user.PremiumExpiryDate,
            Plan = user.Plan.ToString(),
        };
    }

    public async Task<SubscriptionStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var userId = _current.UserId ?? throw new UnauthorizedAccessException();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return new SubscriptionStatusDto
        {
            IsPremium = user?.IsPremium ?? false,
            PremiumExpiryDate = user?.PremiumExpiryDate,
            Plan = user?.Plan.ToString() ?? "Free",
        };
    }

    /// <summary>
    /// Geliştirme/test amaçlı manuel premium aktifleştirme. Yalnızca SuperAdmin
    /// çağırabilir (controller policy'si). Gerçek satın alma mobil IAP + verify akışıyledir.
    /// </summary>
    public async Task<SubscriptionStatusDto> GrantAsync(string productId, CancellationToken ct = default)
    {
        var userId = _current.UserId ?? throw new UnauthorizedAccessException();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        user.Plan = SubscriptionPlan.Premium;
        user.PremiumExpiryDate = (productId ?? "").EndsWith("yearly")
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        await _db.SaveChangesAsync(ct);
        return Map(user);
    }

    private static SubscriptionStatusDto Map(User user) => new()
    {
        IsPremium = user.IsPremium,
        PremiumExpiryDate = user.PremiumExpiryDate,
        Plan = user.Plan.ToString(),
    };
}
