using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Identity;

/// <summary>
/// Implementation of IUserLookupService using ASP.NET Core Identity UserManager.
/// This service bridges the Application layer's need for user data without creating
/// a direct dependency on Infrastructure.Identity types.
/// </summary>
public class UserLookupService : IUserLookupService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserLookupService> _logger;

    public UserLookupService(
        UserManager<ApplicationUser> userManager,
        ILogger<UserLookupService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserDetailsDto?> GetUserDetailsAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            return new UserDetailsDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up user {UserId}", userId);
            return null;
        }
    }
}
