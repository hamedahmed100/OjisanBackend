using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    /// <summary>
    /// Provides access to database-level operations such as transactions.
    /// </summary>
    DatabaseFacade Database { get; }

    DbSet<Group> Groups { get; }

    DbSet<Promotion> Promotions { get; }

    DbSet<GroupMember> GroupMembers { get; }

    DbSet<OrderSubmission> OrderSubmissions { get; }

    DbSet<Product> Products { get; }

    DbSet<ProductOption> ProductOptions { get; }

    DbSet<BadgePosition> BadgePositions { get; }

    DbSet<ProductColor> ProductColors { get; }

    DbSet<ProductAddOn> ProductAddOns { get; }

    DbSet<OrderBadge> OrderBadges { get; }

    DbSet<OrderSubmissionAddOn> OrderSubmissionAddOns { get; }

    DbSet<Payment> Payments { get; }

    DbSet<UserAddress> UserAddresses { get; }

    DbSet<MediaLibrary> MediaLibraries { get; }

    DbSet<MediaLibraryImage> MediaLibraryImages { get; }

    DbSet<ProductMediaLibrary> ProductMediaLibraries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
