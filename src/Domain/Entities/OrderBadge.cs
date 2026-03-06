using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

/// <summary>
/// Badge on a jacket order. Each badge has an image and a mandatory comment.
/// </summary>
public class OrderBadge : BaseAuditableEntity
{
    /// <summary>
    /// Foreign key to the order submission (jacket order).
    /// </summary>
    public int OrderSubmissionId { get; set; }

    /// <summary>
    /// URL of the uploaded badge image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory comment for this badge.
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    public OrderSubmission OrderSubmission { get; set; } = null!;
}
