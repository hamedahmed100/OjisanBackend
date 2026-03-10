using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// IImageUploadService implementation that uploads to Contabo Object Storage (S3-compatible API).
/// No files are stored on local disk.
/// </summary>
public class S3ImageUploadService : IImageUploadService
{
    private const string BadgesFolder = "uploads/badges";
    private readonly IObjectStorageService _storage;
    private readonly ILogger<S3ImageUploadService> _logger;

    public S3ImageUploadService(IObjectStorageService storage, ILogger<S3ImageUploadService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = contentType?.Contains("jpeg", StringComparison.OrdinalIgnoreCase) == true ? ".jpg" : ".png";

        extension = extension.ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            _logger.LogWarning("Rejected upload with invalid extension: {FileName}", fileName);
            throw new ArgumentException("Invalid file extension. Only PNG and JPEG are allowed.", nameof(fileName));
        }

        return await _storage.UploadAsync(
            imageStream,
            fileName,
            contentType ?? "application/octet-stream",
            BadgesFolder,
            cancellationToken);
    }
}
