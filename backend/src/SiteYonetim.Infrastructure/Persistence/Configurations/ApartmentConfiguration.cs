using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> b)
    {
        b.ToTable("apartments");
        b.HasKey(x => x.Id);

        b.Property(x => x.DoorNumber).IsRequired().HasMaxLength(20);
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Block)
         .WithMany(bl => bl.Apartments)
         .HasForeignKey(x => x.BlockId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ApartmentType)
         .WithMany(at => at.Apartments)
         .HasForeignKey(x => x.ApartmentTypeId)
         .IsRequired(false)
         .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.MonthlyDues).HasPrecision(18, 2);

        // Bir blokta kapı no + kat benzersiz.
        b.HasIndex(x => new { x.BlockId, x.DoorNumber, x.Floor }).IsUnique();
        b.HasIndex(x => x.SiteId);     // denormalize SiteId → tenant filtresi için
        b.HasIndex(x => x.BlockId);
        b.HasIndex(x => x.ApartmentTypeId);
    }
}
