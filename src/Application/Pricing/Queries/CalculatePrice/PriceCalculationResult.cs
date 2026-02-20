namespace OjisanBackend.Application.Pricing.Queries.CalculatePrice;

/// <summary>
/// Result of price calculation including payment split information.
/// </summary>
public record PriceCalculationResult
{
    /// <summary>
    /// Total price including base price and all selected options.
    /// </summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// Amount required upfront (either full payment or 50% for large groups).
    /// </summary>
    public decimal UpfrontAmount { get; init; }

    /// <summary>
    /// Remaining amount to be paid later (0 for small groups, 50% for large groups).
    /// </summary>
    public decimal RemainingAmount { get; init; }

    /// <summary>
    /// Whether this order qualifies for partial payment (50/50 split).
    /// </summary>
    public bool IsPartialPayment { get; init; }
}
