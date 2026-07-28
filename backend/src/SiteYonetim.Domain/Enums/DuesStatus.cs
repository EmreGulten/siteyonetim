namespace SiteYonetim.Domain.Enums;

/// <summary>
/// Aidat ödeme durumu. Raporlama ve "borçlu daireler" filtrelemesinde kullanılır.
/// </summary>
public enum DuesStatus
{
    /// <summary>Henüz ödenmedi (tam borç).</summary>
    Unpaid = 0,

    /// <summary>Kısmi ödeme yapıldı (PaidAmount &lt; Amount).</summary>
    PartiallyPaid = 1,

    /// <summary>Tamamen ödendi.</summary>
    Paid = 2,
}
