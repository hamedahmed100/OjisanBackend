using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(g => g.InviteCode)
            .IsRequired()
            .HasMaxLength(20);

        // Create unique index on InviteCode for fast lookups
        builder.HasIndex(g => g.InviteCode)
            .IsUnique();

        builder.Property(g => g.LeaderUserId)
            .IsRequired()
            .HasMaxLength(450); // Standard Identity user ID length

        builder.Property(g => g.BaseDesignJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(g => g.TrelloCardId)
            .HasMaxLength(100);

        builder.Property(g => g.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(g => g.ShippingLabelUrl)
            .HasMaxLength(500);

        // Configure the one-to-many relationship with GroupMember
        // Map to the private backing field _members
        var membersNavigation = builder.HasMany<GroupMember>("_members")
            .WithOne()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata;

        // Configure the navigation property to use field access mode
        membersNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Configure the one-to-many relationship with OrderSubmission
        // Map to the private backing field _submissions
        var submissionsNavigation = builder.HasMany<OrderSubmission>("_submissions")
            .WithOne()
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata;

        // Configure the navigation property to use field access mode
        submissionsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
