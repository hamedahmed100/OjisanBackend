using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class OrderSubmissionConfiguration : IEntityTypeConfiguration<OrderSubmission>
{
    public void Configure(EntityTypeBuilder<OrderSubmission> builder)
    {
        builder.Property(os => os.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard Identity user ID length

        builder.Property(os => os.CustomDesignJson)
            .IsRequired();

        builder.Property(os => os.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(os => os.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(os => os.AdminFeedback)
            .HasMaxLength(2000);

        builder.Property(os => os.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(os => os.ShippingLabelUrl)
            .HasMaxLength(500);

        // Create index on GroupId and UserId for efficient lookups
        builder.HasIndex(os => new { os.GroupId, os.UserId });
    }
}
