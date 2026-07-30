using SiteYonetim.Application.DTOs.Reports;

namespace SiteYonetim.Application.Abstractions;

/// <summary>Yıllık mali bilanço PDF renderer'ı (Premium özellik).</summary>
public interface IBalancePdfRenderer
{
    byte[] Render(AnnualBalanceData data);
}
