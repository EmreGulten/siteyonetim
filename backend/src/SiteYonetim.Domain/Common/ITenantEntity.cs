namespace SiteYonetim.Domain.Common;

/// <summary>
/// Multi-tenancy işaretçisi. Bu arayüzü uygulayan tüm entity'lere
/// DbContext seviyesinde <c>HasQueryFilter</c> uygulanır: bir yönetici
/// yalnızca kendi <see cref="SiteId"/> değerine sahip kayıtları görür.
/// </summary>
public interface ITenantEntity
{
    /// <summary>Bağlı olduğu site/apartman (kiracı) kimliği.</summary>
    Guid SiteId { get; set; }
}
