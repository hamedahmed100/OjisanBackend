using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

/// <summary>
/// Junction: selected add-on for an order submission.
/// </summary>
public class OrderSubmissionAddOn : BaseAuditableEntity
{
    public int OrderSubmissionId { get; set; }
    public int ProductAddOnId { get; set; }

    public OrderSubmission OrderSubmission { get; set; } = null!;
    public ProductAddOn ProductAddOn { get; set; } = null!;
}
