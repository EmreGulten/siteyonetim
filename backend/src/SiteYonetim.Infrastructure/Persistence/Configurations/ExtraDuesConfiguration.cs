using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class ExtraDuesConfiguration : IEntityTypeConfiguration<ExtraDues>
{
    public void Configure(EntityTypeBuilder<ExtraDues> b)
    {
        b.ToTable("extra_dues");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.InstallmentCount).HasDefaultValue(1);
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Site)
         .WithMany(s => s.ExtraDues)
         .HasForeignKey(x => x.SiteId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.SiteId);
        b.HasIndex(x => new { x.IsActive, x.StartDate });
    }
}
