using OjisanBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OjisanBackend.Infrastructure.Data.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.Property(gm => gm.UserId)
            .IsRequired()
            .HasMaxLength(450); // Standard Identity user ID length

        builder.Property(gm => gm.JoinedAt)
            .IsRequired();

        // Create unique index to prevent duplicate memberships
        builder.HasIndex(gm => new { gm.GroupId, gm.UserId })
            .IsUnique();

        // Note: The relationship with Group is configured in GroupConfiguration.cs
        // (the aggregate root) to avoid conflicting mappings. We only configure
        // properties and indexes here.
    }
}
