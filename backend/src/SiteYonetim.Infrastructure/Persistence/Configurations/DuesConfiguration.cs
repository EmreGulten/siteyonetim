using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class DuesConfiguration : IEntityTypeConfiguration<Dues>
{
    public void Configure(EntityTypeBuilder<Dues> b)
    {
        b.ToTable("dues");
        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ReceiptUrl).HasMaxLength(512);
        b.Property(x => x.SiteId).IsRequired();

        // 🔹 PostgreSQL JSONB: aidat dökümü (örn. {"base":500,"extra":250,"exemption":-100})
        b.Property(x => x.Breakdown)
         .HasColumnType("jsonb");

        b.HasOne(x => x.Apartment)
         .WithMany(a => a.Dues)
         .HasForeignKey(x => x.ApartmentId)
         .OnDelete(DeleteBehavior.Restrict);

        // Bir daire için ay+yıl tek kayıt.
        b.HasIndex(x => new { x.ApartmentId, x.Year, x.Month }).IsUnique();
        b.HasIndex(x => x.SiteId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.Year, x.Month });
    }
}
