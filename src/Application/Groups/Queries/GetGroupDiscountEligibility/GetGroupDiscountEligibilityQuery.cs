using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Groups.Common;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Groups.Queries.GetGroupDiscountEligibility;

/// <summary>
/// Request to check if the given product, member count, and options (uniform color) are eligible for a discount,
/// and to get the full price breakdown. Use this so the frontend can show "You save X" and eligibility before creating the group.
/// </summary>
public record GetGroupDiscountEligibilityQuery : IRequest<GroupDiscountEligibilityResult>
{
    /// <summary>
    /// Product public ID (from the catalog, e.g. GetActiveProducts).
    /// </summary>
    public Guid ProductPublicId { get; init; }

    /// <summary>
    /// Number of members (2–30). Discount requires more than 5.
    /// </summary>
    public int MemberCount { get; init; }

    /// <summary>
    /// Whether uniform colour is selected (Jacket, Sleeves, مطاط الاكمام). Required for discount.
    /// </summary>
    public bool IsUniformColorSelected { get; init; }

    /// <summary>
    /// Optional add-on public IDs for accurate total. Add-ons are never discounted.
    /// </summary>
    public List<Guid> AddOnIds { get; init; } = new();
}

/// <summary>
/// Result: product discount percentage (and eligibility/promotion info). No price breakdown.
/// </summary>
public record GroupDiscountEligibilityResult
{
    /// <summary>
    /// True when MemberCount &gt; 5 and IsUniformColorSelected and an active promotion exists (not expired).
    /// </summary>
    public bool IsEligibleForDiscount { get; init; }

    /// <summary>
    /// Promotion name when eligible (e.g. "Uniform Colour 15% Off").
    /// </summary>
    public string? PromotionName { get; init; }

    /// <summary>
    /// Product discount percentage when eligible (e.g. 15). 0 when not eligible.
    /// </summary>
    public decimal DiscountPercent { get; init; }

    /// <summary>
    /// When the promotion ends (UTC). Null when not eligible.
    /// </summary>
    public DateTime? PromotionEndDateUtc { get; init; }
}

public class GetGroupDiscountEligibilityQueryHandler : IRequestHandler<GetGroupDiscountEligibilityQuery, GroupDiscountEligibilityResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IGroupPricingService _groupPricingService;

    public GetGroupDiscountEligibilityQueryHandler(
        IApplicationDbContext context,
        IGroupPricingService groupPricingService)
    {
        _context = context;
        _groupPricingService = groupPricingService;
    }

    public async Task<GroupDiscountEligibilityResult> Handle(GetGroupDiscountEligibilityQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductPublicId && p.IsActive, cancellationToken);

        if (product == null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Product), request.ProductPublicId);
        }

        var asOfUtc = DateTime.UtcNow;
        var pricingResult = await _groupPricingService.CalculateGroupPricingAsync(
            product.Id,
            request.MemberCount,
            request.IsUniformColorSelected,
            request.AddOnIds,
            asOfUtc,
            cancellationToken);

        var promotion = pricingResult.AppliedPromotion;

        return new GroupDiscountEligibilityResult
        {
            IsEligibleForDiscount = pricingResult.Breakdown.PromotionApplied,
            PromotionName = promotion?.PromotionName,
            DiscountPercent = pricingResult.Breakdown.AppliedDiscountPercentage,
            PromotionEndDateUtc = promotion?.EndDate
        };
    }
}
