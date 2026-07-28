using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> b)
    {
        b.ToTable("blocks");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Site)
         .WithMany(s => s.Blocks)
         .HasForeignKey(x => x.SiteId)
         .OnDelete(DeleteBehavior.Restrict);

        // Bir sitede blok adı benzersiz.
        b.HasIndex(x => new { x.SiteId, x.Name }).IsUnique();
        b.HasIndex(x => x.SiteId);
    }
}
