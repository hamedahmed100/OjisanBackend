using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class ProductAddOnConfiguration : IEntityTypeConfiguration<ProductAddOn>
{
    public void Configure(EntityTypeBuilder<ProductAddOn> builder)
    {
        builder.Property(pa => pa.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(pa => pa.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(pa => pa.Price).HasPrecision(18, 2).IsRequired();

        builder.HasOne(pa => pa.Product)
            .WithMany()
            .HasForeignKey(pa => pa.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pa => pa.ProductId);
    }
}
