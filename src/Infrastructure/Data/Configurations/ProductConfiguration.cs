using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.BasePrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.IsActive)
            .IsRequired();

        // Configure the one-to-many relationship with ProductOption
        builder.HasMany(p => p.Options)
            .WithOne()
            .HasForeignKey(po => po.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the one-to-many relationship with BadgePosition
        builder.HasMany(p => p.BadgePositions)
            .WithOne()
            .HasForeignKey(bp => bp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
