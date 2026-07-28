using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Auth;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.Services;

/// <summary>
/// Kimlik doğrulama servisi. Giriş/kayıt → JWT üretimi (SiteId + Role claim ile),
/// refresh token rotasyonu. Başarısız deneme kilidi (brute-force koruması).
/// </summary>
public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        // Anonim istek: tenant filtresini atla (e-posta global benzersiz).
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == req.Email, ct))
            throw new InvalidOperationException("Bu e-posta zaten kayıtlı.");

        // GÜVENLİK: Self-service kayıt her zaman yeni bir SiteManager + site oluşturur.
        // Rol asla client'tan gelmez (eskiden req.Role ile {"role":0} → SuperAdmin).
        var site = new Site
        {
            Name = req.SiteName ?? req.FullName + " Sitesi",
            Slug = Guid.NewGuid().ToString("N")[..8],
        };
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(ct);

        var user = new User
        {
            Email = req.Email,
            FullName = req.FullName,
            Phone = req.Phone,
            PasswordHash = _hasher.Hash(req.Password),
            Role = UserRole.SiteManager,
            SiteId = site.Id,
            IsEmailVerified = false,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);     // user.Id oluşur

        var resp = BuildResponse(user);     // refresh token set
        await _db.SaveChangesAsync(ct);     // refresh token kalıcı (mobil ile aynı)
        return resp;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        // Giriş anonimdir: tenant henüz bilinmiyor → filtre atlanır (kiracı kullanıcıdan belirlenir).
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == req.Email, ct)
            ?? throw new UnauthorizedAccessException("E-posta veya parola hatalı.");

        // Hesap kilidi
        if (user.LockedUntil is { } locked && locked > DateTime.UtcNow)
            throw new UnauthorizedAccessException("Hesap geçici olarak kilitli. Daha sonra tekrar deneyin.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("E-posta veya parola hatalı.");
        }

        // Başarılı → sıfırla
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        var resp = BuildResponse(user);    // refresh token'ı user'a yazar (DB ile aynı)
        await _db.SaveChangesAsync(ct);
        return resp;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Geçersiz refresh token.");

        if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Oturum süresi dolmuş. Tekrar giriş yapın.");

        var resp = BuildResponse(user);    // yeni refresh token üretir + user'a yazar
        await _db.SaveChangesAsync(ct);
        return resp;
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokens.GenerateAccessToken(user.Id, user.Email, user.SiteId, user.Role);
        // DİKKAT: response'daki refresh token ile DB'deki BİRBİRİYLE AYNI olmalı.
        // GenerateAccessToken yeni bir refresh üretir → onu user'a yaz; çağıran kaydeder.
        // (Eski kod ayrı bir refresh üretiyordu → mobil/DB uyuşmazlığı → "Geçersiz refresh token".)
        user.RefreshToken = token.RefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            SiteId = user.SiteId,
            IsPremium = user.IsPremium,
            Token = token,
        };
    }
}
