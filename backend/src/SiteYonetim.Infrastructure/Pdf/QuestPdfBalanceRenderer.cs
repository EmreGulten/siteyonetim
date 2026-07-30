using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Reports;

namespace SiteYonetim.Infrastructure.Pdf;

/// <summary>
/// QuestPDF ile yıllık mali bilanço PDF'i üretir: aylık gelir/gider/net tablosu
/// + yıllık toplamlar. Premium özellik.
/// </summary>
public class QuestPdfBalanceRenderer : IBalancePdfRenderer
{
    static QuestPdfBalanceRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = false;
    }

    public byte[] Render(AnnualBalanceData d)
    {
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var brand = Colors.Blue.Darken2;

        var document = Document.Create(container => container.Page(page =>
        {
            page.Margin(40);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(ts => ts.FontSize(11).FontFamily(Fonts.Arial));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(d.SiteName).SemiBold().FontSize(16).FontColor(brand);
                    row.ConstantItem(190).AlignRight().Column(c =>
                    {
                        c.Item().Text($"YILLIK BİLANÇO {d.Year}").SemiBold().FontSize(13);
                        c.Item().Text($"Tarih: {d.IssuedAt:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(brand);
            });

            page.Content().PaddingVertical(14).Column(col =>
            {
                col.Spacing(6);

                col.Item().Background(Colors.Grey.Lighten4).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("Ay").SemiBold().FontSize(10);
                    r.ConstantItem(110).AlignRight().Text("Gelir (₺)").SemiBold().FontSize(10);
                    r.ConstantItem(110).AlignRight().Text("Gider (₺)").SemiBold().FontSize(10);
                    r.ConstantItem(110).AlignRight().Text("Net (₺)").SemiBold().FontSize(10);
                });

                foreach (var row in d.Rows)
                {
                    var monthName = culture.DateTimeFormat.GetMonthName(row.Month);
                    col.Item().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Row(r =>
                    {
                        r.RelativeItem().Text(monthName).FontSize(10);
                        r.ConstantItem(110).AlignRight().Text($"{row.Income:N2}").FontSize(10);
                        r.ConstantItem(110).AlignRight().Text($"{row.Expense:N2}").FontSize(10);
                        r.ConstantItem(110).AlignRight().Text($"{row.Net:N2}").FontSize(10);
                    });
                }

                col.Item().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("TOPLAM").SemiBold().FontSize(11);
                    r.ConstantItem(110).AlignRight().Text($"{d.TotalIncome:N2}").SemiBold().FontSize(10).FontColor(Colors.Green.Medium);
                    r.ConstantItem(110).AlignRight().Text($"{d.TotalExpense:N2}").SemiBold().FontSize(10).FontColor(Colors.Red.Medium);
                    r.ConstantItem(110).AlignRight().Text($"{d.NetBalance:N2}").SemiBold().FontSize(11).FontColor(brand);
                });
            });

            page.Footer().AlignCenter()
                .Text($"Bu belge elektronik olarak üretilmiştir · {d.SiteName} · {d.IssuedAt:dd.MM.yyyy}")
                .FontSize(8).FontColor(Colors.Grey.Medium);
        }));

        return document.GeneratePdf();
    }
}
