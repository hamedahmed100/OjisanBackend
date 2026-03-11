using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// Contabo Object Storage implementation using the S3-compatible API.
/// All uploads go to your Contabo bucket (eu2.contabostorage.com); no data is sent to Amazon.
/// </summary>
public class S3ObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3ObjectStorageService> _logger;
    private readonly string _publicBaseUrl;

    public S3ObjectStorageService(
        IOptions<S3StorageOptions> options,
        ILogger<S3ObjectStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _publicBaseUrl = GetPublicBaseUrl();

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint.TrimEnd('/'),
            ForcePathStyle = _options.ForcePathStyle,
            SignatureVersion = "v4"
        };

        _s3Client = new AmazonS3Client(
            _options.AccessKey,
            _options.SecretKey,
            config);
    }

    private string GetPublicBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return _options.PublicBaseUrl.TrimEnd('/');

        var endpoint = _options.Endpoint.TrimEnd('/');
        return _options.ForcePathStyle
            ? $"{endpoint}/{_options.Bucket}"
            : endpoint.Replace("https://", $"https://{_options.Bucket}.");
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folderPrefix,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = contentType?.Contains("jpeg", StringComparison.OrdinalIgnoreCase) == true ? ".jpg" : ".png";

        extension = extension.ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var key = $"{folderPrefix.Trim('/')}/{uniqueFileName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType ?? "application/octet-stream",
            AutoCloseStream = false,
            CannedACL = S3CannedACL.PublicRead
        };

        try
        {
            await _s3Client.PutObjectAsync(request, cancellationToken);
            var url = $"{_publicBaseUrl}/{key}";
            _logger.LogInformation("Uploaded file to S3: {Key}", key);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3: {Key}", key);
            throw;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        var key = ExtractKeyFromUrlOrKey(objectKey);

        try
        {
            await _s3Client.DeleteObjectAsync(_options.Bucket, key, cancellationToken);
            _logger.LogInformation("Deleted object from S3: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object from S3: {Key}", key);
            throw;
        }
    }

    private string ExtractKeyFromUrlOrKey(string urlOrKey)
    {
        if (string.IsNullOrWhiteSpace(urlOrKey))
            return urlOrKey;

        if (urlOrKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || urlOrKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(urlOrKey);
                var path = uri.AbsolutePath.TrimStart('/');
                var bucketPrefix = $"{_options.Bucket}/";
                if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
                    path = path[bucketPrefix.Length..];
                return path;
            }
            catch
            {
                return urlOrKey;
            }
        }

        return urlOrKey;
    }
}
