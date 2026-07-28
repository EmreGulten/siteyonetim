using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Daire bazlı muafiyet (aidat muaf). Belirli tarih aralığında dairenin
/// aidatı 0 hesaplanır. <see cref="DuesGenerationService"/> bu tabloya bakar.
/// </summary>
public class Exemption : TenantEntity
{
    public Guid ApartmentId { get; set; }
    public Apartment? Apartment { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Muafiyet sebebi (örn. "Boş daire", "Yönetim kurulu kararı").</summary>
    public string? Reason { get; set; }

    /// <summary>Tam muaf mı, yoksa indirim oranı mı? 1.0 = tam muaf, 0.5 = %50.</summary>
    public decimal DiscountRatio { get; set; } = 1.0m;

    public bool IsActive { get; set; } = true;
}
