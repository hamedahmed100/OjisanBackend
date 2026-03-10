using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class MediaLibraryConfiguration : IEntityTypeConfiguration<MediaLibrary>
{
    public void Configure(EntityTypeBuilder<MediaLibrary> builder)
    {
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.HasMany(x => x.Images)
            .WithOne(x => x.MediaLibrary)
            .HasForeignKey(x => x.MediaLibraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

