using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Persistence.Configurations;

/// <summary>
/// Gelir/Gider (Income/Expense) tek tablolu modeli. DocumentUrl MinIO yoludur.
/// </summary>
public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> b)
    {
        b.ToTable("transactions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Category).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.DocumentUrl).HasMaxLength(512);
        b.Property(x => x.SiteId).IsRequired();

        b.HasOne(x => x.Site)
         .WithMany(s => s.Transactions)
         .HasForeignKey(x => x.SiteId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SiteId, x.Date });
        b.HasIndex(x => new { x.SiteId, x.Type, x.Date });
        b.HasIndex(x => x.RelatedDuesId);
    }
}
