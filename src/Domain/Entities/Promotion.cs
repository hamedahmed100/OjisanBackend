using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

/// <summary>
/// Promotion for group orders (e.g. uniform colour discount).
/// Used to check eligibility and expiry before applying discount at group creation.
/// </summary>
public class Promotion : BaseAuditableEntity
{
    /// <summary>
    /// Display name of the promotion (e.g. "Uniform Colour 15% Off").
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Discount percentage applied when eligible (e.g. 15).
    /// Applied only to base product price, not add-ons.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// When the promotion becomes valid.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// When the promotion is no longer valid. Check DateTime.UtcNow &lt; EndDate before applying.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Minimum group size (member count) required to be eligible (e.g. 6 for "5+ rule").
    /// </summary>
    public int MinGroupSize { get; set; }

    /// <summary>
    /// Whether this promotion is currently active (soft delete / toggle).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
