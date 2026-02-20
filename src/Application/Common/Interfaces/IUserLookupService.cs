namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for looking up user details from Identity.
/// This interface exists in the Application layer to avoid violating Clean Architecture
/// by having Application depend on Infrastructure.Identity types.
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Gets user details by user ID.
    /// </summary>
    /// <param name="userId">The user ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User details if found, null otherwise.</returns>
    Task<UserDetailsDto?> GetUserDetailsAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>
/// DTO containing user details needed for notifications.
/// </summary>
public record UserDetailsDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}
