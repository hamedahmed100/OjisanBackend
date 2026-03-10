namespace OjisanBackend.Application.MediaLibraries.Common;

public class MediaLibraryImageDto
{
    public Guid PublicId { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;
}

public class MediaLibraryDto
{
    public Guid PublicId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTimeOffset Created { get; init; }

    public IReadOnlyCollection<MediaLibraryImageDto> Images { get; init; } = Array.Empty<MediaLibraryImageDto>();
}

public class CreateMediaLibraryResult
{
    public Guid PublicId { get; init; }

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<MediaLibraryImageDto> Images { get; init; } = Array.Empty<MediaLibraryImageDto>();
}

public class AdminMediaLibraryProductDto
{
    public Guid PublicId { get; init; }

    public string Name { get; init; } = string.Empty;
}

public class AdminMediaLibraryDto
{
    public Guid PublicId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTimeOffset Created { get; init; }

    public IReadOnlyCollection<AdminMediaLibraryProductDto> Products { get; init; } = Array.Empty<AdminMediaLibraryProductDto>();

    public int TotalImageCount { get; init; }

    public IReadOnlyCollection<MediaLibraryImageDto> Images { get; init; } = Array.Empty<MediaLibraryImageDto>();
}

