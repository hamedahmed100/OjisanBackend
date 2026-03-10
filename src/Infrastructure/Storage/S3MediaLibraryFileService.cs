using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// IMediaLibraryFileService implementation that stores files in Contabo Object Storage (S3-compatible API).
/// SaveFileAsync returns the full public URL. DeleteFileAsync accepts URL or object key.
/// </summary>
public class S3MediaLibraryFileService : IMediaLibraryFileService
{
    private const string LibrariesFolder = "uploads/libraries";
    private readonly IObjectStorageService _storage;
    private readonly ILogger<S3MediaLibraryFileService> _logger;

    public S3MediaLibraryFileService(IObjectStorageService storage, ILogger<S3MediaLibraryFileService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = contentType?.Contains("jpeg", StringComparison.OrdinalIgnoreCase) == true ? ".jpg" : ".png";

        extension = extension.ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            _logger.LogWarning("Rejected media library upload with invalid extension: {FileName}", fileName);
            throw new ArgumentException("Invalid file extension. Only PNG and JPEG are allowed.", nameof(fileName));
        }

        return await _storage.UploadAsync(content, fileName, contentType, LibrariesFolder, cancellationToken);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        return _storage.DeleteAsync(relativePath, cancellationToken);
    }
}
