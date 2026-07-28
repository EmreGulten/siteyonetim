using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Domain.Common;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Enums;

namespace SiteYonetim.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın DbContext'i. Sağladıkları:
///  - Snake_case PostgreSQL tablo/kolon isimlendirmesi
///  - Multi-tenant + soft-delete global query filter
///  - <c>xmin</c> tabanlı iyimser eşzamanlılık (tüm tablolar)
///  - Audit alanlarının SaveChanges sırasında otomatik doldurulması
/// Kiracı bilgisi <see cref="ICurrentUserService"/> (JWT claim tabanlı) üzerinden alınır.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService _current;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService current)
        : base(options)
    {
        _current = current;
    }

    // ─── DbSets ──────────────────────────────────────────────────────────
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<ApartmentType> ApartmentTypes => Set<ApartmentType>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Dues> Dues => Set<Dues>();
    public DbSet<ExtraDues> ExtraDues => Set<ExtraDues>();
    public DbSet<ExtraDuesDifference> ExtraDuesDifferences => Set<ExtraDuesDifference>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<Exemption> Exemptions => Set<Exemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1) Tüm IEntityTypeConfiguration sınıflarını uygula (şema + ilişkiler).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 2) Snake_case + xmin concurrency (owned/JSON tipleri atlanır)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            ApplySnakeCase(entityType);
            AddXminConcurrencyToken(entityType);
        }

        ApplyQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        base.ConfigureConventions(configurationBuilder);
    }

    // ─── Query filter'ları merkezî kurar ─────────────────────────────────
    // KRİTİK: filtreler _current'a DOĞRUDAN (lambda gövdesinde) referans verir.
    // EF Core, DbContext üye erişimlerini HER SORGUDA mevcut context örneğinden değerlendirir.
    // Değeri local değişkene çıkarırsan (constant), model oluşturma anında (örn. başlangıç
    // migration'ında _current boşken) sabitlenip cache'lenir → tüm isteklerde yanlış kalır.
    private void ApplyQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>().HasQueryFilter(s =>
            !s.IsDeleted && (_current.Role == UserRole.SuperAdmin || s.Id == (_current.SiteId ?? Guid.Empty)));

        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted && (_current.Role == UserRole.SuperAdmin || u.SiteId == (_current.SiteId ?? Guid.Empty)));

        // TenantEntity türevleri (SiteId her sorguda mevcut context'ten değerlendirilir)
        AddTenantFilter<Block>(modelBuilder);
        AddTenantFilter<ApartmentType>(modelBuilder);
        AddTenantFilter<Apartment>(modelBuilder);
        AddTenantFilter<Resident>(modelBuilder);
        AddTenantFilter<Dues>(modelBuilder);
        AddTenantFilter<ExtraDues>(modelBuilder);
        AddTenantFilter<FinancialTransaction>(modelBuilder);
        AddTenantFilter<Exemption>(modelBuilder);

        // Tenant-scoped olmayan (ExtraDuesDifference): yalnız soft-delete
        modelBuilder.Entity<ExtraDuesDifference>().HasQueryFilter(e => !e.IsDeleted);
    }

    private void AddTenantFilter<T>(ModelBuilder mb) where T : TenantEntity
    {
        mb.Entity<T>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (_current.Role == UserRole.SuperAdmin || e.SiteId == (_current.SiteId ?? Guid.Empty)));
    }

    private static void ApplySnakeCase(IMutableEntityType entityType)
    {
        entityType.SetTableName(ToSnakeCase(entityType.GetTableName() ?? entityType.DisplayName()));
        foreach (var property in entityType.GetProperties())
            property.SetColumnName(ToSnakeCase(property.Name));
        foreach (var key in entityType.GetKeys())
            key.SetName(ToSnakeCase(key.GetName() ?? $"{entityType.GetTableName()}_pk"));
        foreach (var index in entityType.GetIndexes())
            index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
        foreach (var fk in entityType.GetForeignKeys())
            fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()));
    }

    private static void AddXminConcurrencyToken(IMutableEntityType entityType)
    {
        var xmin = entityType.FindProperty("xmin") ?? entityType.AddProperty("xmin", typeof(uint));
        xmin.SetColumnType("xid");
        xmin.IsConcurrencyToken = true;
        xmin.ValueGenerated = ValueGenerated.OnAddOrUpdate;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void OnBeforeSave()
    {
        var now = DateTime.UtcNow;
        var uid = _current.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    if (entry.Entity.CreatedBy == null) entry.Entity.CreatedBy = uid;
                    if (entry.Entity is ITenantEntity tenant && tenant.SiteId == Guid.Empty
                        && _current.SiteId is { } siteId && _current.Role != UserRole.SuperAdmin)
                    {
                        tenant.SiteId = siteId;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = uid;
                    break;
                case EntityState.Deleted: // soft-delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }

    private static string ToSnakeCase(string? name) =>
        name is null ? string.Empty
        : Regex.Replace(Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1_$2"),
                        @"([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}
