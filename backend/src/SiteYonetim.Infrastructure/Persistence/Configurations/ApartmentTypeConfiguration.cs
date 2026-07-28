using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class ApartmentTypeConfiguration : IEntityTypeConfiguration<ApartmentType>
{
    public void Configure(EntityTypeBuilder<ApartmentType> b)
    {
        b.ToTable("apartment_types");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.BaseDues).HasPrecision(18, 2);
        b.Property(x => x.ArsaPayi).HasPrecision(10, 6); // oran
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Site)
         .WithMany(s => s.ApartmentTypes)
         .HasForeignKey(x => x.SiteId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SiteId, x.Name }).IsUnique();
        b.HasIndex(x => x.SiteId);
    }
}
