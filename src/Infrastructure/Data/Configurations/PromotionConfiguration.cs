using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Property(p => p.PromotionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate)
            .IsRequired();

        builder.HasIndex(p => new { p.IsActive, p.StartDate, p.EndDate })
            .HasFilter("[IsActive] = 1");
    }
}
