using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SiteYonetim.Application.Abstractions;

namespace SiteYonetim.Infrastructure.Storage;

/// <summary>
/// MinIO (S3 uyumlu) nesne depolama servisi. Dosyayı GUID ile yeniden adlandırıp yükler,
/// DB'de saklanacak genel erişilebilir URL döndürür. Bucket yoksa başlangıçta oluşturulur.
/// </summary>
public class MinioStorageService : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _opt;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioOptions> opt, ILogger<MinioStorageService> logger)
    {
        _opt = opt.Value;
        _logger = logger;
        _client = new MinioClient()
            .WithEndpoint(_opt.Endpoint)
            .WithCredentials(_opt.AccessKey, _opt.SecretKey)
            .WithSSL(_opt.UseSsl)
            .Build();
    }

    /// <summary>Uygulama başlangıcında çağrılır; bucket var eder.</summary>
    public async Task EnsureBucketAsync(CancellationToken ct = default)
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_opt.Bucket), ct).ConfigureAwait(false);
        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_opt.Bucket), ct).ConfigureAwait(false);
            _logger.LogInformation("MinIO bucket oluşturuldu: {Bucket}", _opt.Bucket);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct).ConfigureAwait(false);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_opt.Bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType), ct).ConfigureAwait(false);

        return ToPublicUrl(objectName);
    }

    public async Task<string> UploadBytesAsync(byte[] bytes, string objectName, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(bytes);
        return await UploadAsync(ms, objectName, contentType, ct).ConfigureAwait(false);
    }

    public async Task<Stream> DownloadAsync(string objectName, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_opt.Bucket)
            .WithObject(objectName)
            .WithCallbackStream(s => s.CopyTo(ms)), ct).ConfigureAwait(false);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteAsync(string objectName, CancellationToken ct = default) =>
        _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_opt.Bucket).WithObject(objectName), ct);

    private string ToPublicUrl(string objectName) =>
        string.IsNullOrWhiteSpace(_opt.PublicBaseUrl)
            ? $"/storage/{Uri.EscapeDataString(objectName)}"
            : $"{_opt.PublicBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(objectName)}";
}
