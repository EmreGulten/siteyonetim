using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

/// <summary>
/// Site (kiracı) konfigürasyonu.
/// PostgreSQL avantajları: <c>Settings</c> JSONB, <c>Tags</c> <c>text[]</c>.
/// </summary>
public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> b)
    {
        b.ToTable("sites");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.Phone).HasMaxLength(32);

        // 🔹 PostgreSQL JSONB: site ayarları (şemasız, indekslenebilir)
        b.OwnsOne(x => x.Settings, s =>
        {
            s.ToJson("settings");              // Npgsql 8: JSONB sütunu
            s.Property(p => p.Currency).HasMaxLength(3);
            s.Property(p => p.BrandColor).HasMaxLength(9);
        });

        // 🔹 PostgreSQL Array (text[]): etiketler (Npgsql List<string> → text[] native)
        b.Property(x => x.Tags).HasColumnType("text[]");

        b.Property(x => x.ApartmentCount).HasDefaultValue(0);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Email);
        // Tags üzerinde GIN indeksi (array containment sorgularını hızlandırır).
        b.HasIndex(x => x.Tags).HasMethod("gin");

        // Query filter'lar (soft-delete + tenant) AppDbContext'te merkezî kurulur.
    }
}
