using System.Reflection;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OjisanBackend.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    public DbSet<OrderSubmission> OrderSubmissions => Set<OrderSubmission>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();

    public DbSet<BadgePosition> BadgePositions => Set<BadgePosition>();

    public DbSet<ProductColor> ProductColors => Set<ProductColor>();

    public DbSet<ProductAddOn> ProductAddOns => Set<ProductAddOn>();

    public DbSet<OrderBadge> OrderBadges => Set<OrderBadge>();

    public DbSet<OrderSubmissionAddOn> OrderSubmissionAddOns => Set<OrderSubmissionAddOn>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    public DbSet<MediaLibrary> MediaLibraries => Set<MediaLibrary>();

    public DbSet<MediaLibraryImage> MediaLibraryImages => Set<MediaLibraryImage>();

    public DbSet<ProductMediaLibrary> ProductMediaLibraries => Set<ProductMediaLibrary>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
