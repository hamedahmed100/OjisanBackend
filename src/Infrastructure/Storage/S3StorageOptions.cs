namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// Configuration for Contabo Object Storage (S3-compatible API).
/// Data is stored in your Contabo bucket only; nothing is sent to Amazon.
/// Environment variables: S3_ACCESS_KEY, S3_SECRET_KEY, S3_BUCKET, S3_ENDPOINT, S3_PUBLIC_BASE_URL.
/// </summary>
public class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "photos";
    public string Endpoint { get; set; } = "https://eu2.contabostorage.com";
    /// <summary>
    /// Base URL for public object URLs (e.g. https://eu2.contabostorage.com/photos).
    /// If not set, derived from Endpoint and Bucket.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
    /// <summary>
    /// Use path-style bucket URLs (required for Contabo).
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(Bucket)
        && !string.IsNullOrWhiteSpace(Endpoint);
}
