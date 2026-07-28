using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Site içindeki blok (A blok, B blok ...). Tüm daireler bir bloğa bağlıdır.
/// </summary>
public class Block : TenantEntity
{
    /// <summary>Blok adı (örn. "A Blok").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Listeleme sırası.</summary>
    public int DisplayOrder { get; set; }

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public Site? Site { get; set; }
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
