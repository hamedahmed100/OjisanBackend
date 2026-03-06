using OjisanBackend.Application.Groups.Common;

namespace OjisanBackend.Application.Groups.Commands.CreateGroup;

/// <summary>
/// Result of group creation including group id and pricing breakdown for the frontend.
/// </summary>
public record CreateGroupResult
{
    public Guid GroupId { get; init; }
    public GroupPriceBreakdownDto PriceBreakdown { get; init; } = null!;
}
