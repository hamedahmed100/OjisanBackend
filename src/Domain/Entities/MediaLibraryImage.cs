using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

public class MediaLibraryImage : BaseAuditableEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int MediaLibraryId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public MediaLibrary? MediaLibrary { get; set; }
}

