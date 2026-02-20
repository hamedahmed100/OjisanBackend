using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.TransactionId)
            .HasMaxLength(200);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.IsPartial)
            .IsRequired();

        builder.Property(p => p.Phase)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Create index on GroupId for efficient lookups
        builder.HasIndex(p => p.GroupId);

        // Create index on TransactionId for webhook lookups
        builder.HasIndex(p => p.TransactionId);
    }
}
