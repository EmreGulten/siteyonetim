using SiteYonetim.Domain.Common;
using SiteYonetim.Domain.ValueObjects;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Multi-tenancy'nin ana bağlayıcısı (kiracı). Her site/apartman bir kayıttır.
/// Tüm tenant-scoped tablolar <see cref="ITenantEntity.SiteId"/> üzerinden buraya bağlanır.
/// </summary>
public class Site : BaseEntity
{
    /// <summary>Site/apartman adı (örn. "Güneş Sitesi").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL/arama için benzersiz kısa ad.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Address { get; set; }

    /// <summary>Yönetici/iletişim telefonu.</summary>
    public string? Phone { get; set; }

    public string? Email { get; set; }

    // ─── PostgreSQL JSONB: esnek, şemasız ayarlar ───────────────────────
    /// <summary>
    /// Site ayarları (para birimi, aidat günü, branding, IBAN ...).
    /// JSONB olarak saklanır; yeni alan eklemek için migration gerekmez.
    /// </summary>
    public SiteSettings Settings { get; set; } = new();

    // ─── PostgreSQL Array (text[]): çoklu etiket ────────────────────────
    /// <summary>
    /// Etiketler (örn. ["park", "asansor", "guvenlik"]). PostgreSQL <c>text[]</c> sütunu;
    /// <c>ARRAY_CONTAINS</c> / <c>ANY</c> ile indeksli sorgulanabilir.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Toplam bağımsız bölüm sayısı (cache/raporlama için).</summary>
    public int ApartmentCount { get; set; }

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    // ─── Navigasyonlar ───────────────────────────────────────────────────
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Block> Blocks { get; set; } = new List<Block>();
    public ICollection<ApartmentType> ApartmentTypes { get; set; } = new List<ApartmentType>();
    public ICollection<ExtraDues> ExtraDues { get; set; } = new List<ExtraDues>();
    public ICollection<FinancialTransaction> Transactions { get; set; } = new List<FinancialTransaction>();
}
