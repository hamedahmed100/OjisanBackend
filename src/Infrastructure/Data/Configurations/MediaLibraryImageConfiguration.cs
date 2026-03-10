using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class MediaLibraryImageConfiguration : IEntityTypeConfiguration<MediaLibraryImage>
{
    public void Configure(EntityTypeBuilder<MediaLibraryImage> builder)
    {
        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(x => x.MediaLibraryId);
    }
}

