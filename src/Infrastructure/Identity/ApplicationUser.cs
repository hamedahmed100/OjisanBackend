using Microsoft.AspNetCore.Identity;

namespace OjisanBackend.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }
}
