using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        builder.Property(pc => pc.NameAr).IsRequired().HasMaxLength(100);
        builder.Property(pc => pc.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(pc => pc.HexCode).IsRequired().HasMaxLength(20);
        builder.Property(pc => pc.Type).IsRequired().HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(pc => pc.Type);
    }
}
