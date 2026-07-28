namespace SiteYonetim.Application.Abstractions;

/// <summary>
/// S3-uyumlu nesne depolama (MinIO) soyutlaması.
/// Görseller/faturalar DB'ye değil, buraya yüklenir; DB'de sadece URL saklanır.
/// </summary>
public interface IFileStorage
{
    /// <summary>Dosyayı yükler, GUID ile yeniden adlandırır, genel erişilebilir URL döndürür.</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Dosya baytlarını indirir (örn. üretilen makbuz PDF'i).</summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);

    /// <summary>PDF/byte[] direkt yüklemek (sunucu-üretimi içerikler için).</summary>
    Task<string> UploadBytesAsync(byte[] bytes, string fileName, string contentType, CancellationToken ct = default);

    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
