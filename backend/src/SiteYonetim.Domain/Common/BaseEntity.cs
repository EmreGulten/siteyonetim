namespace SiteYonetim.Domain.Common;

/// <summary>
/// Tüm entity'ler için ortak temel sınıf. Güvenlik/audit amaçlı alanlar
/// ile PostgreSQL <c>xmin</c> tabanlı iyimser eşzamanlılık (concurrency) sağlar.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Genel benzersiz kimlik (sıralı/numaratik olmadığı için enumeration saldırısına kapalı).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Silinme bayrağı (soft delete). Multi-tenant sistemlerde fiziksel silme yerine
    /// işaret koymak, denetim ve yanlışlıkla veri kaybını önler.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>Soft-delete tarihi.</summary>
    public DateTime? DeletedAt { get; set; }
}
