using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Groups.Common;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Infrastructure.Services;

/// <summary>
/// Calculates group pricing with optional promotion. Discount applies only to base product price.
/// Formula when eligible: (BasePrice * (1 - Discount/100) * Count) + (AddonsPrice * Count).
/// </summary>
public class GroupPricingService : IGroupPricingService
{
    private readonly IApplicationDbContext _context;

    public GroupPricingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GroupPricingResult> CalculateGroupPricingAsync(
        int productId,
        int memberCount,
        bool isUniformColorSelected,
        IReadOnlyList<Guid> addOnPublicIds,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found or is not active.");

        decimal addonTotalPerMember = 0m;
        if (addOnPublicIds.Count > 0)
        {
            var addOns = await _context.ProductAddOns
                .AsNoTracking()
                .Where(pa => addOnPublicIds.Contains(pa.PublicId) && (pa.ProductId == null || pa.ProductId == productId))
                .ToListAsync(cancellationToken);

            if (addOns.Count != addOnPublicIds.Count)
            {
                var found = addOns.Select(pa => pa.PublicId).ToHashSet();
                var missing = addOnPublicIds.First(id => !found.Contains(id));
                throw new InvalidOperationException($"Add-on with ID {missing} not found or not valid for this product.");
            }

            addonTotalPerMember = addOns.Sum(a => a.Price);
        }

        decimal basePrice = product.BasePrice;
        decimal originalProductTotal = basePrice * memberCount;
        decimal addonTotal = addonTotalPerMember * memberCount;
        decimal discountedProductTotal = originalProductTotal;
        decimal discountPercent = 0m;
        Promotion? appliedPromotion = null;

        // Eligibility: MemberCount > 5 AND IsUniformColorSelected AND active promotion not expired
        bool eligibleForDiscount = memberCount >= 5 && isUniformColorSelected;

        if (eligibleForDiscount)
        {
            var promotion = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive
                    && p.MinGroupSize <= memberCount
                    && p.StartDate <= asOfUtc
                    && p.EndDate > asOfUtc)
                .OrderByDescending(p => p.DiscountPercent)
                .FirstOrDefaultAsync(cancellationToken);

            if (promotion != null)
            {
                discountPercent = promotion.DiscountPercent;
                appliedPromotion = promotion;
                discountedProductTotal = basePrice * (1 - discountPercent / 100m) * memberCount;
            }
        }

        decimal discountAmount = originalProductTotal - discountedProductTotal;
        decimal subtotal = originalProductTotal + addonTotal;
        decimal finalTotal = discountedProductTotal + addonTotal;

        var breakdown = new GroupPriceBreakdownDto
        {
            OriginalProductPrice = originalProductTotal,
            DiscountedProductPrice = discountedProductTotal,
            AddonPrice = addonTotal,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            FinalTotal = finalTotal,
            PromotionApplied = appliedPromotion != null,
            AppliedDiscountPercentage = discountPercent
        };

        return new GroupPricingResult
        {
            Breakdown = breakdown,
            AppliedPromotion = appliedPromotion
        };
    }
}
