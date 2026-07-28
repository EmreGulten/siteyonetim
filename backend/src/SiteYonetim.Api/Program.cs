using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SiteYonetim.Api.Middleware;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Domain.Enums;
using SiteYonetim.Infrastructure;
using SiteYonetim.Infrastructure.Identity;
using SiteYonetim.Infrastructure.Jobs;
using SiteYonetim.Infrastructure.Persistence;
using SiteYonetim.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ─── 1) Servisler ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Merkezî istisna middleware'i (IMiddleware → DI'a kayıtlı olmalı)
builder.Services.AddSingleton<SiteYonetim.Api.Middleware.GlobalExceptionHandler>();

// Clean Architecture: Infrastructure katmanı (DB + JWT + MinIO + servisler + job'lar)
builder.Services.AddInfrastructure(builder.Configuration);

// ─── 2) JWT Authentication ───────────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt ayarları eksik.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// ─── 3) Role / Policy Based Authorization ────────────────────────────
builder.Services.AddAuthorization(options =>
{
    // Sadece site yöneticileri veri girişi yapabilir.
    options.AddPolicy("SiteManager", p => p.RequireRole(
        nameof(UserRole.SiteManager), nameof(UserRole.SuperAdmin)));
    options.AddPolicy("SuperAdmin", p => p.RequireRole(nameof(UserRole.SuperAdmin)));
});

// ─── 4) CORS: yalnızca mobil + izinli domainler ──────────────────────
var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("MobileAndPanel", p =>
    p.WithOrigins(allowedOrigins.Length > 0 ? allowedOrigins : new[] { "*" })
     .AllowAnyHeader().AllowAnyMethod().AllowAnyHeader()));

// ─── 5) Rate Limiting (IP başına 60/dk) — FAZ 6 ──────────────────────
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
    o.AddPolicy("per-ip", ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new()
        {
            TokenLimit = 60,
            TokensPerPeriod = 60,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
});

// ─── 6) Hangfire (aidat üretim background jobs) ──────────────────────
builder.Services.AddHangfire(c => c
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

// ─── 7) Swagger (JWT destekli) ───────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Site & Apartman Yönetim API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: 'Bearer {token}'",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ─── 8) Başlangıç: migration + MinIO bucket + Hangfire kaydı ─────────
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    try
    {
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        // MinIO bucket var et
        var storage = sp.GetRequiredService<IFileStorage>();
        if (storage is MinioStorageService minio)
            await minio.EnsureBucketAsync();

        // Hangfire recurring: her ayın 1'i gece 02:00'da aidat üret
        RecurringJob.AddOrUpdate<DuesGenerationJob>(
            "dues-monthly", j => j.RunMonthlyAsync(), "0 2 1 * *", new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey") });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Başlangıç hazırlığı sırasında hata.");
    }
}

// ─── 9) Pipeline ─────────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionHandler>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MobileAndPanel");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthFilter() }, // yalnızca SuperAdmin (basit)
});

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow })).AllowAnonymous();

app.Run();
