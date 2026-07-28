using SiteYonetim.Application.DTOs.Reports;

namespace SiteYonetim.Application.Abstractions;

/// <summary>KMK ihtarname PDF üretimi (QuestPDF). Premium özellik.</summary>
public interface IIhtarnamePdfRenderer
{
    /// <summary>Verilen borç verisinden yasal ihtarname PDF'i (byte[]) üretir.</summary>
    byte[] Render(IhtarnameData data);
}
