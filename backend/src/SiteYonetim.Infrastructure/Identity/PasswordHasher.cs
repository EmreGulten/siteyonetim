using SiteYonetim.Application.Abstractions;

namespace SiteYonetim.Infrastructure.Identity;

/// <summary>BCrypt tabanlı parola hash/doğrulama.</summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
