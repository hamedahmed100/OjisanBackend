namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for integrating with Trello API to create production cards.
/// </summary>
public interface ITrelloService
{
    /// <summary>
    /// Creates a Trello card for the order with member details and badge images.
    /// </summary>
    /// <param name="details">Order details including group info, members, and submissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Trello card ID.</returns>
    Task<string> CreateCardAsync(OrderDetailsDto details, CancellationToken cancellationToken);
}

/// <summary>
/// DTO containing order details for Trello card creation.
/// </summary>
public record OrderDetailsDto
{
    public Guid GroupId { get; init; }
    public string LeaderUserId { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public List<MemberSubmissionDto> MemberSubmissions { get; init; } = new();
}

/// <summary>
/// DTO for member submission details in Trello card.
/// </summary>
public record MemberSubmissionDto
{
    public string UserId { get; init; } = string.Empty;
    public string BadgeImageUrl { get; init; } = string.Empty;
    public string CustomDesignJson { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
