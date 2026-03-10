using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

public class MediaLibrary : BaseAuditableEntity
{
    private readonly List<MediaLibraryImage> _images = new();

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyCollection<MediaLibraryImage> Images => _images.AsReadOnly();

    public void AddImage(MediaLibraryImage image)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        image.MediaLibraryId = Id;
        _images.Add(image);
    }
}

