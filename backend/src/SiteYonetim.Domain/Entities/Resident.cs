using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Daire sakini (malik veya kiracı). <see cref="UserId"/> nullable'dır:
/// uygulamayı kullanan sakinler buraya bağlı User kaydına sahiptir.
/// </summary>
public class Resident : TenantEntity
{
    public Guid ApartmentId { get; set; }
    public Apartment? Apartment { get; set; }

    /// <summary>Uygulama kullanıcısı (nullable — kayıtlı değilse null).</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// TC Kimlik No. Listeleme API'lerinde yalnızca son 4 hanesi gösterilir
    /// (maskelendirme — FAZ 6). Şifreli/at-rest saklanması önerilir.
    /// </summary>
    public string? TcNo { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    /// <summary>Malik mi (ev sahibi)?</summary>
    public bool IsOwner { get; set; }

    /// <summary>Kiracı mı?</summary>
    public bool IsTenant { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>İkamet başlangıcı.</summary>
    public DateTime? MoveInDate { get; set; }

    /// <summary>Tahliye tarihi (geçmiş sakin kayıtları için).</summary>
    public DateTime? MoveOutDate { get; set; }
}
