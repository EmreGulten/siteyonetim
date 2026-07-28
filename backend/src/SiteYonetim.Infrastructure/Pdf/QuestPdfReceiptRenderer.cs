using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiteYonetim.Application.Abstractions;

namespace SiteYonetim.Infrastructure.Pdf;

/// <summary>
/// QuestPDF ile sunucu taraflı tahsilat makbuzu üretimi. Mobil HTML template
/// tutmak yerine profesyonel PDF burada üretilir, mobil byte[] indirir.
/// </summary>
public class QuestPdfReceiptRenderer : IReceiptPdfRenderer
{
    static QuestPdfReceiptRenderer()
    {
        // Topluluk/Açık kaynak lisansı (ticari kullanım için QuestPDF lisansı gerekir).
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = false;
    }

    public byte[] RenderReceipt(ReceiptData d)
    {
        var brand = TryParseColor(d.BrandColor, Colors.Green.Darken2);
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var monthName = culture.DateTimeFormat.GetMonthName(d.Month);

        var document = Document.Create(container => container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A5);
            page.DefaultTextStyle(ts => ts.FontSize(11).FontFamily(Fonts.Arial));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(d.SiteName).SemiBold().FontSize(18).FontColor(brand);
                    row.ConstantItem(120).AlignRight().Text($"Makbuz No\n{d.ReceiptNumber}").FontSize(9);
                });
                if (!string.IsNullOrWhiteSpace(d.ManagerTitle))
                    col.Item().Text(d.ManagerTitle).FontSize(10);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(brand);
            });

            page.Content().PaddingVertical(14).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text("TAHSİLAT MAKBUZU").SemiBold().FontSize(13);
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(InfoText("Daire", $"{d.BlockName} / {d.DoorNumber}"));
                    r.RelativeItem().Text(InfoText("Sakin", d.ResidentName));
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(InfoText("Dönem", $"{monthName} {d.Year}"));
                    r.RelativeItem().Text(InfoText("Tahsilat Tarihi", d.PaidDate.ToString("dd.MM.yyyy", culture)));
                });

                col.Item().PaddingTop(8);
                col.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Toplam Aidat");
                        r.ConstantItem(90).AlignRight().Text(Money(d.Amount, d.Currency)).SemiBold();
                    });
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Ödenen Tutar").FontColor(brand);
                        r.ConstantItem(90).AlignRight().Text(Money(d.PaidAmount, d.Currency)).SemiBold().FontColor(brand);
                    });
                });

                if (d.IsFreePlan)
                {
                    col.Item().PaddingTop(8).AlignCenter()
                        .Text("Ücretsiz Plan ile Oluşturuldu")
                        .FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                }
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Line($"Bu makbuz elektronik olarak üretilmiştir. {d.SiteName}");
                t.Hyperlink("https://sitendeavoir.com", "https://sitendeavoir.com").FontSize(8);
            });
        }));

        return document.GeneratePdf();
    }

    private static string Money(decimal v, string cur) => $"{v:N2} {cur}";
    private static string InfoText(string label, string value) => $"{label}: {value}";

    private static string TryParseColor(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        return hex.StartsWith("#") ? hex : fallback;
    }
}
