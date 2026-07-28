using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Abstractions;

/// <summary>
/// Mevcut istek bağlamındaki kullanıcı bilgisini sağlar (JWT claim'lerinden).
/// Application katmanı bu abstraction'a bağlıdır; implementasyon Infrastructure'da.
/// AppDbContext bu arayüzü multi-tenant global query filter için kullanır.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? SiteId { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
