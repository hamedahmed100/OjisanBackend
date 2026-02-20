using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.Property(po => po.Value)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.AdditionalCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(po => po.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Create index on ProductId and Category for efficient lookups
        builder.HasIndex(po => new { po.ProductId, po.Category });
    }
}
