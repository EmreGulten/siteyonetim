using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Premium özellik geçidi (FAZ 5.1 özellik haritası).
/// Ücretsiz plan sınırlarını uygular:
///   - Tek site, en fazla 20 daire
///   - Ek aidat yönetimi YOK
///   - Raporlarda reklam, makbuza watermark
/// Premium: sınırsız site/daire, ek aidat, reklamsız, KMK raporu, özel makbuz.
/// </summary>
public class PremiumPolicy
{
    public const int FreeMaxApartments = 20;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public PremiumPolicy(IAppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    /// <summary>Mevcut kullanıcı premium mu? (süresi dolmuşsa free'e düşer).</summary>
    public async Task<bool> IsPremiumAsync(CancellationToken ct = default)
    {
        if (_current.UserId is null) return false;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == _current.UserId, ct);
        return user?.IsPremium ?? false;
    }

    /// <summary>Free plan limitini aşma kontrolü (daire ekleme öncesi).</summary>
    public async Task EnsureCanAddApartmentAsync(CancellationToken ct = default)
    {
        if (await IsPremiumAsync(ct)) return;
        if (_current.SiteId is null) return;

        var count = await _db.Apartments.CountAsync(a => a.SiteId == _current.SiteId, ct);
        if (count >= FreeMaxApartments)
            throw new InvalidOperationException(
                $"Ücretsiz planda en fazla {FreeMaxApartments} daire ekleyebilirsiniz. Premium'a geçin.");
    }

    /// <summary>Ek aidat yalnızca Premium. Free planda engellenir.</summary>
    public async Task EnsureCanManageExtraDuesAsync(CancellationToken ct = default)
    {
        if (!await IsPremiumAsync(ct))
            throw new InvalidOperationException("Ek aidat yönetimi Premium abonelik gerektirir.");
    }
}
