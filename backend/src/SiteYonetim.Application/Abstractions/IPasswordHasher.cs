namespace SiteYonetim.Application.Abstractions;

/// <summary>Parola hash/doğrula (BCrypt/Argon2). Düz metin saklanmaz.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
