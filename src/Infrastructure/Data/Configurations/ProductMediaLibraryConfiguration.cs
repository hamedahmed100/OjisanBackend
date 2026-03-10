using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class ProductMediaLibraryConfiguration : IEntityTypeConfiguration<ProductMediaLibrary>
{
    public void Configure(EntityTypeBuilder<ProductMediaLibrary> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.MediaLibraryId });

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MediaLibrary)
            .WithMany()
            .HasForeignKey(x => x.MediaLibraryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProductId);
    }
}

