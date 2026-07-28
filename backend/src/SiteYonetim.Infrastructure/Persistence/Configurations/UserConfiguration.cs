using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

/// <summary>
/// User konfigürasyonu. <see cref="User.SiteId"/> nullable (SuperAdmin için).
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.Plan).HasConversion<int>();
        b.Property(x => x.RefreshToken).HasMaxLength(512);
        b.Property(x => x.StoreSubscriptionId).HasMaxLength(256);

        // Site ilişkisi: SuperAdmin (SiteId = null) dahil tüm kullanıcılar.
        b.HasOne(x => x.Site)
         .WithMany(s => s.Users)
         .HasForeignKey(x => x.SiteId)
         .OnDelete(DeleteBehavior.Restrict);

        // Email bir site içinde değil, global olarak benzersiz (giriş e-postası).
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.SiteId);
        b.HasIndex(x => x.Role);
    }
}
