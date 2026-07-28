using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Application.Abstractions;

/// <summary>
/// DbContext soyutlaması. Application servisleri bunu kullanır;
/// <c>SiteYonetim.Infrastructure.Persistence.AppDbContext</c> uygular.
/// Bu sayede Application → Infrastructure bağımlılığı tersine çevrilir (Clean Architecture).
/// </summary>
public interface IAppDbContext
{
    DbSet<Site> Sites { get; }
    DbSet<User> Users { get; }
    DbSet<Block> Blocks { get; }
    DbSet<ApartmentType> ApartmentTypes { get; }
    DbSet<Apartment> Apartments { get; }
    DbSet<Resident> Residents { get; }
    DbSet<Dues> Dues { get; }
    DbSet<ExtraDues> ExtraDues { get; }
    DbSet<ExtraDuesDifference> ExtraDuesDifferences { get; }
    DbSet<FinancialTransaction> Transactions { get; }
    DbSet<Exemption> Exemptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
