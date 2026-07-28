using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiteYonetim.Application.Abstractions;
using SiteYonetim.Application.DTOs.Reports;

namespace SiteYonetim.Infrastructure.Pdf;

/// <summary>
/// QuestPDF ile KMK uyumlu ihtarname PDF'i üretir. Borç dökümü + 634 sayılı KMK
/// ve TMK m.684 atıflı ihtar metni içerir. Premium özellik.
/// </summary>
public class QuestPdfIhtarnameRenderer : IIhtarnamePdfRenderer
{
    static QuestPdfIhtarnameRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableCaching = false;
    }

    public byte[] Render(IhtarnameData d)
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
                    row.ConstantItem(160).AlignRight().Column(c =>
                    {
                        c.Item().Text("İHTARNAME").SemiBold().FontSize(14);
                        c.Item().Text($"Tarih: {d.IssuedAt:dd.MM.yyyy}").FontSize(9);
                        c.Item().Text($"Evrak No: ITH-{d.IssuedAt:yyyyMMdd}-{d.ApartmentLabel.GetHashCode().ToString("x").ToUpper()}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
                if (!string.IsNullOrWhiteSpace(d.SiteAddress))
                    col.Item().Text(d.SiteAddress).FontSize(9).FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(brand);
            });

            page.Content().PaddingVertical(14).Column(col =>
            {
                col.Spacing(8);

                col.Item().Text("BORÇLU").SemiBold().FontColor(brand).FontSize(10);
                col.Item().Text(t =>
                {
                    t.Line($"Ad Soyad: {d.OwnerName ?? "-"}");
                    t.Line($"Bağımsız Bölüm: {d.ApartmentLabel}");
                    if (!string.IsNullOrWhiteSpace(d.OwnerTcMasked)) t.Line($"T.C. Kimlik No: {d.OwnerTcMasked}");
                    if (!string.IsNullOrWhiteSpace(d.Phone)) t.Line($"Telefon: {d.Phone}");
                });

                col.Item().PaddingTop(2).Text($"AİDAT BORÇ DÖKÜMÜ ({d.Year})").SemiBold().FontColor(brand).FontSize(10);

                // Başlık satırı
                col.Item().Background(Colors.Grey.Lighten4).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("Dönem").SemiBold().FontSize(10);
                    r.ConstantItem(120).AlignRight().Text("Kalan Borç (₺)").SemiBold().FontSize(10);
                });
                foreach (var row in d.Rows)
                {
                    var monthName = culture.DateTimeFormat.GetMonthName(row.Month);
                    col.Item().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Row(r =>
                    {
                        r.RelativeItem().Text($"{monthName} {row.Year}").FontSize(10);
                        r.ConstantItem(120).AlignRight().Text($"{row.Remaining:N2}").FontSize(10);
                    });
                }
                col.Item().Background(Colors.Grey.Lighten3).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("TOPLAM BORÇ").SemiBold().FontSize(11);
                    r.ConstantItem(120).AlignRight().Text($"{d.TotalDebt:N2} ₺").SemiBold().FontSize(11).FontColor(brand);
                });

                col.Item().PaddingTop(10).Text("İHTAR METNİ").SemiBold().FontColor(brand).FontSize(10);
                col.Item().Text("Yukarıda açık kimlik ve adresi yazılı bağımsız bölüm malikine; yukarıdaki dökümde belirtilen tutarda aidat borcunun ödenmesi ihtar olunur.").FontSize(10);
                col.Item().Text($"Bu ihtarnamenin tebliği tarihinden itibaren 30 (otuz) gün içinde toplam {d.TotalDebt:N2} ₺ borcun ödenmemesi halinde, 634 sayılı Kat Mülkiyeti Kanunu'nun 19, 20 ve 21'inci maddeleri ile Türk Medenî Kanunu'nun 684'üncü maddesi uyarınca icra ve hukuki yollara başvurulacak, ayrıca yargılama gideri ve vekalet ücreti borçludan tahsil edilecektir.").FontSize(10);

                col.Item().PaddingTop(28).AlignRight().Column(c =>
                {
                    c.Item().Text($"İhtar Eden: {d.ManagerName ?? "Site Yönetimi"}").FontSize(10);
                    c.Item().PaddingTop(28).Text("İmza / Kaşe").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            page.Footer().AlignCenter()
                .Text($"Bu belge elektronik olarak üretilmiştir · {d.SiteName} · {d.IssuedAt:dd.MM.yyyy}")
                .FontSize(8).FontColor(Colors.Grey.Medium);
        }));

        return document.GeneratePdf();
    }
}
