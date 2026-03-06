namespace OjisanBackend.Application.Groups.Common;

/// <summary>
/// Detailed pricing breakdown for group creation so the frontend can show savings (product vs add-ons).
/// Discount applies only to base product price; add-ons are never discounted.
/// </summary>
public record GroupPriceBreakdownDto
{
    /// <summary>
    /// Total product price before discount (BasePrice * MemberCount).
    /// </summary>
    public decimal OriginalProductPrice { get; init; }

    /// <summary>
    /// Total product price after discount. Equals OriginalProductPrice when no discount is applied.
    /// </summary>
    public decimal DiscountedProductPrice { get; init; }

    /// <summary>
    /// Total add-ons price (sum of selected add-on prices * MemberCount).
    /// </summary>
    public decimal AddonPrice { get; init; }

    /// <summary>
    /// Amount saved on the product (OriginalProductPrice - DiscountedProductPrice).
    /// </summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>
    /// Subtotal before discount (OriginalProductPrice + AddonPrice).
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// Final total: DiscountedProductPrice + AddonPrice.
    /// </summary>
    public decimal FinalTotal { get; init; }

    /// <summary>
    /// Whether a promotion was applied (e.g. 15% off base product).
    /// </summary>
    public bool PromotionApplied { get; init; }

    /// <summary>
    /// Discount percentage applied to base product (e.g. 15). 0 if no discount.
    /// </summary>
    public decimal AppliedDiscountPercentage { get; init; }
}
