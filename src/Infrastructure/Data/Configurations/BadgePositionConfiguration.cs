using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class BadgePositionConfiguration : IEntityTypeConfiguration<BadgePosition>
{
    public void Configure(EntityTypeBuilder<BadgePosition> builder)
    {
        builder.Property(bp => bp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bp => bp.IsRequired)
            .IsRequired();

        // Create index on ProductId for efficient lookups
        builder.HasIndex(bp => bp.ProductId);
    }
}
