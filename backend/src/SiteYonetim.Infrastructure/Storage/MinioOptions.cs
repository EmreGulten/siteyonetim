namespace SiteYonetim.Infrastructure.Storage;

public class MinioOptions
{
    public const string SectionName = "Minio";
    public string Endpoint { get; set; } = "minio:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "siteyonetim-docs";
    public bool UseSsl { get; set; }
    /// <summary>Dış erişilebilir base URL (makbuz PDF/nesne linkleri için).</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
