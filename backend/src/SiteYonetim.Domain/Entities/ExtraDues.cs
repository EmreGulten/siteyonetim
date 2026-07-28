using SiteYonetim.Domain.Common;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Ek aidat kampanyası (örn. "Asansör yenileme", "Bahçe bakımı").
/// Belirli tarih aralığında, <see cref="InstallmentCount"/> taksit halinde,
/// daire tipine göre farklı tutarlarda (<see cref="ExtraDuesDifference"/>) tahsil edilir.
/// Premium özellik (FAZ 5).
/// </summary>
public class ExtraDues : TenantEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Taksit sayısı (toplam tutar buna bölünerek aylara yayılır).</summary>
    public int InstallmentCount { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    // ─── Navigasyon ─────────────────────────────────────────────────────
    public Site? Site { get; set; }
    public ICollection<ExtraDuesDifference> Differences { get; set; } = new List<ExtraDuesDifference>();
}
