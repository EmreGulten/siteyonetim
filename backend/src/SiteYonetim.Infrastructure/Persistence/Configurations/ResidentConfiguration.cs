using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

public class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> b)
    {
        b.ToTable("residents");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.TcNo).HasMaxLength(32); // TC no: listelemede maskelenecek (FAZ 6)
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Apartment)
         .WithMany(a => a.Residents)
         .HasForeignKey(x => x.ApartmentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.User)
         .WithMany(u => u.ResidentProfiles)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.ApartmentId);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.TcNo);          // TC ile arama
        b.HasIndex(x => x.Phone);
        b.HasIndex(x => x.SiteId);
    }
}
