using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class OrderBadgeConfiguration : IEntityTypeConfiguration<OrderBadge>
{
    public void Configure(EntityTypeBuilder<OrderBadge> builder)
    {
        builder.Property(ob => ob.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(ob => ob.Comment).IsRequired().HasMaxLength(500);

        builder.HasOne(ob => ob.OrderSubmission)
            .WithMany(os => os.Badges)
            .HasForeignKey(ob => ob.OrderSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ob => ob.OrderSubmissionId);
    }
}
