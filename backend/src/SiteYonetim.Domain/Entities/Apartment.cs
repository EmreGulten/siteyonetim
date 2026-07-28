using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Bağımsız bölüm (daire/dükkan/depo). Blok + kapı no + kat bilgisi tanımlar.
/// Daire tipi opsiyoneldir; aidat <see cref="MonthlyDues"/> (daireye özel) üzerinden hesaplanır,
/// yoksa tipin BaseDues'una düşer.
/// <see cref="SiteId"/> denormalize edilmiştir → multi-tenant global query filter doğrudan çalışır.
/// </summary>
public class Apartment : TenantEntity
{
    public Guid BlockId { get; set; }
    public Block? Block { get; set; }

    /// <summary>Daire tipi (opsiyonel — artık zorunlu değil).</summary>
    public Guid? ApartmentTypeId { get; set; }
    public ApartmentType? ApartmentType { get; set; }

    /// <summary>Daireye özel aylık aidat (₺). Aidat üretimi bunu kullanır.</summary>
    public decimal MonthlyDues { get; set; }

    /// <summary>Kapı no (örn. "12", "D-4").</summary>
    public string DoorNumber { get; set; } = string.Empty;

    /// <summary>Bulunduğu kat.</summary>
    public int Floor { get; set; }

    /// <summary>Dolu/boş durumu.</summary>
    public bool IsOccupied { get; set; } = true;

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
    public ICollection<Dues> Dues { get; set; } = new List<Dues>();
    public ICollection<Exemption> Exemptions { get; set; } = new List<Exemption>();

    /// <summary>Yardımcı: o anki aktif malik (IsOwner=true) sakini.</summary>
    public Resident? CurrentOwner => Residents.FirstOrDefault(r => r.IsOwner && r.IsActive);
}
