using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Ek aidatın daire tipine göre tutar farkı. Her ek aidat + daire tipi ikilisi için
/// bir tutar tanımlanır (örn. asansör aidatı: 2+1 → 250₺, 3+1 → 350₺).
/// SiteId ExtraDues üzerinden türetilir (FK üzerinden erişilir).
/// </summary>
public class ExtraDuesDifference : BaseEntity
{
    public Guid ExtraDuesId { get; set; }
    public ExtraDues? ExtraDues { get; set; }

    public Guid ApartmentTypeId { get; set; }
    public ApartmentType? ApartmentType { get; set; }

    /// <summary>Bu daire tipi için taksit başına ek aidat tutarı.</summary>
    public decimal Amount { get; set; }
}
