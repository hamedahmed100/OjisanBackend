namespace OjisanBackend.Domain.Entities;

public class ProductMediaLibrary
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int MediaLibraryId { get; set; }

    public MediaLibrary? MediaLibrary { get; set; }
}

