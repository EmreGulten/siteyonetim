using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.Services;
using SiteYonetim.Infrastructure.Identity;
using SiteYonetim.Infrastructure.Jobs;
using SiteYonetim.Infrastructure.Persistence;
using SiteYonetim.Infrastructure.Pdf;
using SiteYonetim.Infrastructure.Storage;
using SiteYonetim.Infrastructure.Store;

namespace SiteYonetim.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Tüm altyapı katmanı kayıtları (DB + kimlik + depolama + store + job'lar + servisler).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);

        // --- Kimlik / JWT ---
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // --- Nesne depolama (MinIO) ---
        services.AddScoped<IFileStorage, MinioStorageService>();

        // --- PDF üretimi ---
        services.AddSingleton<IReceiptPdfRenderer, QuestPdfReceiptRenderer>();
        services.AddSingleton<IIhtarnamePdfRenderer, QuestPdfIhtarnameRenderer>();
        services.AddSingleton<IBalancePdfRenderer, QuestPdfBalanceRenderer>();

        // --- Store IAP doğrulama: RevenueCat (Apple/Google makbuz doğrulamasını RevenueCat yapar) ---
        services.AddHttpClient("RevenueCat", c => c.BaseAddress = new Uri("https://api.revenuecat.com/"));
        services.Configure<RevenueCatOptions>(configuration.GetSection(RevenueCatOptions.SectionName));
        services.AddScoped<IStoreReceiptVerifier, RevenueCatReceiptVerifier>();

        // --- Background jobs (Hangfire) ---
        services.AddScoped<DuesGenerationJob>();

        // --- Application servisleri ---
        services.AddScoped<AuthService>();
        services.AddScoped<DuesGenerationService>();
        services.AddScoped<FinancialSummaryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<CollectionService>();
        services.AddScoped<ReceiptService>();
        services.AddScoped<ApartmentService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<PremiumPolicy>();

        return services;
    }

    /// <summary>PostgreSQL bağlantısı + DbContext kaydı.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection eksik.");

        // Npgsql 8: Dictionary/POCO → jsonb yazımı için dinamik JSON açık olmalı (Dues.Breakdown)
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }
}
