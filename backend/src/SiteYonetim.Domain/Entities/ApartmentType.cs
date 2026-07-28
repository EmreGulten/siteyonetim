using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Daire tipi. Aidat, <see cref="BaseDues"/> ve <see cref="ArsaPayi"/> (arsa payı)
/// bu tipten hesaplanır. Ek aidat farkları da tipe göre tanımlanır.
/// </summary>
public class ApartmentType : TenantEntity
{
    /// <summary>Tip adı (örn. "2+1", "Dükkan", "3+1 Dubleks").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Temel aylık aidat tutarı (para birimi site ayarından).</summary>
    public decimal BaseDues { get; set; }

    /// <summary>Arsa payı oranı (0.05 = 5 binde vs. kamu payı hesabı).</summary>
    public decimal ArsaPayi { get; set; }

    public bool IsActive { get; set; } = true;

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public Site? Site { get; set; }
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    public ICollection<ExtraDuesDifference> ExtraDuesDifferences { get; set; } = new List<ExtraDuesDifference>();
}
