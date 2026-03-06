using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class OrderSubmissionAddOnConfiguration : IEntityTypeConfiguration<OrderSubmissionAddOn>
{
    public void Configure(EntityTypeBuilder<OrderSubmissionAddOn> builder)
    {
        builder.HasKey(osa => new { osa.OrderSubmissionId, osa.ProductAddOnId });

        builder.HasOne(osa => osa.OrderSubmission)
            .WithMany(os => os.SelectedAddOns)
            .HasForeignKey(osa => osa.OrderSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(osa => osa.ProductAddOn)
            .WithMany()
            .HasForeignKey(osa => osa.ProductAddOnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(osa => osa.OrderSubmissionId);
    }
}
