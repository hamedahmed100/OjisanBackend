namespace OjisanBackend.Application.Common.Interfaces;

public interface IMediaLibraryFileService
{
    Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken);
}

