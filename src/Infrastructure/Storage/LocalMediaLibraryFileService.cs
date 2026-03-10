using Microsoft.Extensions.Configuration;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

public class LocalMediaLibraryFileService : IMediaLibraryFileService
{
    private readonly string _storagePath;
    private readonly string _relativePath;

    public LocalMediaLibraryFileService(IConfiguration configuration)
    {
        _storagePath = configuration["MediaLibrary:StoragePath"]
            ?? throw new InvalidOperationException("MediaLibrary:StoragePath is not configured in appsettings.json.");

        _relativePath = configuration["MediaLibrary:RelativePath"] ?? "uploads/libraries";
    }

    public async Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentType == "image/jpeg" ? ".jpg" : ".png";
        }

        extension = Path.GetExtension(extension).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            throw new ArgumentException("Invalid file extension. Only PNG and JPEG are allowed.", nameof(fileName));
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        var fullPath = Path.Combine(_storagePath, uniqueFileName);

        var normalizedStoragePath = Path.GetFullPath(_storagePath);
        var normalizedFullPath = Path.GetFullPath(fullPath);

        if (!normalizedFullPath.StartsWith(normalizedStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file path detected. Path traversal attack prevented.");
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await content.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);

        return $"{_relativePath.TrimEnd('/')}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var fileName = Path.GetFileName(relativePath);
        var fullPath = Path.Combine(_storagePath, fileName);

        var normalizedStoragePath = Path.GetFullPath(_storagePath);
        var normalizedFullPath = Path.GetFullPath(fullPath);

        if (!normalizedFullPath.StartsWith(normalizedStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file path detected. Path traversal attack prevented.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}

