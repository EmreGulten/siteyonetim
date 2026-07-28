using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class ExemptionConfiguration : IEntityTypeConfiguration<Exemption>
{
    public void Configure(EntityTypeBuilder<Exemption> b)
    {
        b.ToTable("exemptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.DiscountRatio).HasPrecision(5, 4); // 0.0000 - 1.0000
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Apartment)
         .WithMany(a => a.Exemptions)
         .HasForeignKey(x => x.ApartmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ApartmentId);
        b.HasIndex(x => x.SiteId);
        // Çakışan muafiyet kontrolü için tarih aralığı indeksi.
        b.HasIndex(x => new { x.ApartmentId, x.StartDate, x.EndDate });
    }
}
