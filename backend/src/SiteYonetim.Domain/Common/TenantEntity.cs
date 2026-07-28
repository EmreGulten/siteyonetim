namespace SiteYonetim.Domain.Common;

/// <summary>
/// SiteId içeren entity'ler için taban sınıf.
/// Tüm kiracı-tabanlı veriler (Users, Blocks, Apartments, Dues ...) bundan türer.
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid SiteId { get; set; }
}
