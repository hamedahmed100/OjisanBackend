using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Groups.Common;

/// <summary>
/// Calculates group pricing with optional promotion (e.g. 5+ uniform colour discount).
/// Discount applies only to base product price; add-ons are never discounted.
/// </summary>
public interface IGroupPricingService
{
    /// <summary>
    /// Computes pricing breakdown for a group.
    /// Formula when eligible: (BasePrice * (1 - Discount/100) * Count) + (AddonsPrice * Count).
    /// </summary>
    /// <param name="productId">Product internal ID.</param>
    /// <param name="memberCount">Number of members (2–30).</param>
    /// <param name="isUniformColorSelected">Whether uniform colour is selected (required for discount).</param>
    /// <param name="addOnPublicIds">Optional add-on public IDs; their prices are summed per member.</param>
    /// <param name="asOfUtc">Time used to check promotion expiry; typically DateTime.UtcNow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Breakdown and the active promotion used (if any).</returns>
    Task<GroupPricingResult> CalculateGroupPricingAsync(
        int productId,
        int memberCount,
        bool isUniformColorSelected,
        IReadOnlyList<Guid> addOnPublicIds,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of group pricing calculation, including breakdown and optional promotion snapshot.
/// </summary>
public record GroupPricingResult
{
    public GroupPriceBreakdownDto Breakdown { get; init; } = null!;

    /// <summary>
    /// The promotion applied, if any (e.g. for storing DiscountExpiryDate on the group).
    /// </summary>
    public Promotion? AppliedPromotion { get; init; }
}
