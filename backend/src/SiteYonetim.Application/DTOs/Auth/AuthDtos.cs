using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Application.DTOs.Auth;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? SiteName { get; set; } // yeni site kaydı

    // GÜVENLİK: Role bilerek client'tan ALINMAZ. Self-service kayıt her zaman yeni
    // bir SiteManager oluşturur (rol AuthService.RegisterAsync'ta sabitlenir).
    // Eskiden req.Role'e güvenilip {"role":0} ile SuperAdmin yaratılabiliyordu.
}

public sealed class TokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? SiteId { get; set; }
    public bool IsPremium { get; set; }
    public TokenResult Token { get; set; } = new();
}

public sealed class RefreshRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
