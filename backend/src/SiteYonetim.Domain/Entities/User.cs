using SiteYonetim.Domain.Common;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Sistem kullanıcısı. <see cref="UserRole"/> ve <see cref="SiteId"/> claim olarak
/// JWT içine gömülür (FAZ 3). SuperAdmin için <see cref="SiteId"/> null olabilir.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt/Argon2 hash. Asla düz metin saklanmaz.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>Telefon veya e-posta ile doğrulama bayrağı.</summary>
    public bool IsEmailVerified { get; set; }

    public UserRole Role { get; set; } = UserRole.Resident;

    // ─── Multi-tenancy: SuperAdmin'de null olabilir ─────────────────────
    public Guid? SiteId { get; set; }
    public Site? Site { get; set; }

    // ─── Premium (FAZ 5) ────────────────────────────────────────────────
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTime? PremiumExpiryDate { get; set; }
    public bool IsPremium =>
        Plan == SubscriptionPlan.Premium &&
        (PremiumExpiryDate == null || PremiumExpiryDate > DateTime.UtcNow);

    /// <summary>Store receipt tanımlayıcısı (IAP doğrulama sonucu).</summary>
    public string? StoreSubscriptionId { get; set; }

    // ─── JWT Refresh Token (FAZ 3) ──────────────────────────────────────
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Başarısız giriş denemeleri (brute-force koruması).</summary>
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public ICollection<Resident> ResidentProfiles { get; set; } = new List<Resident>();
}
