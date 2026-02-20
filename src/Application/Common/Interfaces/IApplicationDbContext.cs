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

    DbSet<GroupMember> GroupMembers { get; }

    DbSet<OrderSubmission> OrderSubmissions { get; }

    DbSet<Product> Products { get; }

    DbSet<ProductOption> ProductOptions { get; }

    DbSet<BadgePosition> BadgePositions { get; }

    DbSet<Payment> Payments { get; }

    DbSet<UserAddress> UserAddresses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
