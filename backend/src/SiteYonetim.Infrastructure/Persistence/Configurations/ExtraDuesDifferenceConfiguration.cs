using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ek aidat × daire tipi farkı. Tenant-scoped DEĞİL (ExtraDues üzerinden türetilir).
/// </summary>
public class ExtraDuesDifferenceConfiguration : IEntityTypeConfiguration<ExtraDuesDifference>
{
    public void Configure(EntityTypeBuilder<ExtraDuesDifference> b)
    {
        b.ToTable("extra_dues_differences");
        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(18, 2);

        b.HasOne(x => x.ExtraDues)
         .WithMany(e => e.Differences)
         .HasForeignKey(x => x.ExtraDuesId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ApartmentType)
         .WithMany(at => at.ExtraDuesDifferences)
         .HasForeignKey(x => x.ApartmentTypeId)
         .OnDelete(DeleteBehavior.Restrict);

        // Bir ek aidat için bir daire tipi tek kayıt.
        b.HasIndex(x => new { x.ExtraDuesId, x.ApartmentTypeId }).IsUnique();
    }
}
