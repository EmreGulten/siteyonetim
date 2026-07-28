namespace SiteYonetim.Domain.Enums;

/// <summary>
/// Uygulama rolleri. JWT içine claim olarak eklenir (FAZ 3).
/// </summary>
public enum UserRole
{
    /// <summary>Sistem yöneticisi — tüm siteleri görür (SuperAdmin.SiteId = null).</summary>
    SuperAdmin = 0,

    /// <summary>Site/apartman yöneticisi — yalnızca kendi sitesini yönetir.</summary>
    SiteManager = 1,

    /// <summary>Daire sakini — yalnızca kendi daire verisini görür.</summary>
    Resident = 2,
}
