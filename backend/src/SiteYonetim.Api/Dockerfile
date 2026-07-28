# ============================================================
#  SiteYonetim.Api — Multi-stage Dockerfile (.NET 8)
#  1. Aşama: SDK ile restore + build + publish
#  2. Aşama: Runtime-only, küçük ve güvenli final image
# ============================================================

# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Önce sadece proje dosyalarını kopyala (Docker cache katmanı)
# → kaynak değişse bile restore her seferinde çalışmaz.
COPY ["src/SiteYonetim.Api/SiteYonetim.Api.csproj", "src/SiteYonetim.Api/"]
COPY ["src/SiteYonetim.Application/SiteYonetim.Application.csproj", "src/SiteYonetim.Application/"]
COPY ["src/SiteYonetim.Domain/SiteYonetim.Domain.csproj", "src/SiteYonetim.Domain/"]
COPY ["src/SiteYonetim.Infrastructure/SiteYonetim.Infrastructure.csproj", "src/SiteYonetim.Infrastructure/"]

RUN dotnet restore "src/SiteYonetim.Api/SiteYonetim.Api.csproj"

# Tüm kaynak kopyala + publish
COPY . .
WORKDIR "/src/src/SiteYonetim.Api"
RUN dotnet publish "SiteYonetim.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Healthcheck için curl (aspnet slim imajında yoktur)
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Root olmayan kullanıcı ile çalış (FAZ 6 güvenlik)
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

EXPOSE 8080

# Production optimizasyonları
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_TieredPGO=1

COPY --from=build /app/publish ./

# Healthcheck için curl gerekli; root kurulumu sonrası kullanıcı değiştirdik
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SiteYonetim.Api.dll"]
