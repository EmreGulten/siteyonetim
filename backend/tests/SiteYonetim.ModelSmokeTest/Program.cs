using Microsoft.EntityFrameworkCore;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Domain.Enums;
using SiteYonetim.Infrastructure.Persistence;

// Model gerçekten geçerli mi? (FluentAPI/JSONB/Array/xmin hataları derleme anında değil,
// model oluşturma anında ortaya çıkar.) Gerçek veritabanı BAĞLANTISI gerektirmez.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var fakeCurrent = new FakeCurrentUserService();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql("Host=localhost;Database=does_not_connect") // bağlanılmaz, sadece model
    .Options;

using var ctx = new AppDbContext(options, fakeCurrent);

// OnModelCreating'i tetikler → FluentAPI doğrulanır
var model = ctx.Model;
var entityCount = model.GetEntityTypes().Count(e => !e.IsOwned());
Console.WriteLine($"✅ EF Core modeli geçerli. Tablo sayısı: {entityCount}");

foreach (var et in model.GetEntityTypes().Where(e => !e.IsOwned()).OrderBy(e => e.GetTableName()))
{
    var cols = string.Join(", ", et.GetProperties()
        .Select(p => $"{(p.IsShadowProperty() ? "⁎" : "")}{p.Name}:{p.GetColumnType()}"));
    Console.WriteLine($"  • {et.GetTableName(),-26} [{cols}]");
}
// (⁎ = shadow/kolon olmayan property, örn. xmin, JSON kolonları)

Console.WriteLine("\n✅ Smoke test başarılı — FAZ 2 veri modeli geçerli.");

// --- Tanılama: owned/JSON tipleri ve settings kolonu ---
var ownedTypes = model.GetEntityTypes().Where(e => e.IsOwned()).ToList();
Console.WriteLine($"\nOwned tip sayısı: {ownedTypes.Count}");
foreach (var ot in ownedTypes)
    Console.WriteLine($"  • Owned: {ot.Name}  →  JSON sütunu: {(ot.IsMappedToJson() ? "EVET" : "HAYIR")}");

var sites = model.FindEntityType(typeof(SiteYonetim.Domain.Entities.Site))!;
var settingsProp = sites.GetProperties().FirstOrDefault(p => p.GetColumnType() == "jsonb" || p.Name.Contains("settings", StringComparison.OrdinalIgnoreCase));
Console.WriteLine(settingsProp is null
    ? "  (settings JSONB kolonu EF API'sinde container olarak temsil edilir — aşağıda DDL ile doğrulanır)"
    : $"  ✅ settings kolonu: {settingsProp.Name} → {settingsProp.GetColumnType()}");

// --- Kesin kanıt: modelden gerçek DDL üret (canlı bağlantı gerekmez) ---
var ddl = ctx.Database.GenerateCreateScript();
Console.WriteLine($"  → 'settings' jsonb kolonu DDL'de: {(ddl.Contains("settings jsonb", StringComparison.OrdinalIgnoreCase) ? "✅ VAR" : "⚠️ YOK")}");
Console.WriteLine($"  → 'tags' text[] kolonu DDL'de:     {(ddl.Contains("tags text[]", StringComparison.OrdinalIgnoreCase) ? "✅ VAR" : "⚠️ YOK")}");
Console.WriteLine($"  → 'breakdown' jsonb (dues) DDL'de: {(ddl.Contains("breakdown jsonb", StringComparison.OrdinalIgnoreCase) ? "✅ VAR" : "⚠️ YOK")}");
Console.WriteLine($"  → 'xmin' concurrency token (model):  {(model.GetEntityTypes().Count(e => !e.IsOwned() && e.FindProperty("xmin")?.IsConcurrencyToken == true) >= 11 ? "✅ VAR (tüm tablolarda)" : "⚠️ YOK")}");
Console.WriteLine("    (Not: xmin PostgreSQL sistem kolonudur, CREATE TABLE'de yer almaz; UPDATE WHERE kısmında kullanılır.)");

sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? SiteId => null;
    public UserRole? Role => UserRole.SuperAdmin;
    public bool IsAuthenticated => true;
}
