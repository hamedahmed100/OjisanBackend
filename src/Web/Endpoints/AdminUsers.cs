using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Domain.Constants;
using OjisanBackend.Infrastructure.Identity;

namespace OjisanBackend.Web.Endpoints;

public record AdminUserSummaryDto(string Id, string? Email, string? UserName, string Role, bool IsActive, DateTimeOffset MemberSince, DateTimeOffset? LastLoginAt);

public record AdminUserDetailsDto(string Id, string? Email, string? UserName, string Role, bool IsActive, bool EmailConfirmed, DateTimeOffset MemberSince, DateTimeOffset? LastLoginAt);

public record UpdateUserStatusRequest(bool IsActive);

public record UpdateUserRoleRequest(string Role);

public class AdminUsers : EndpointGroupBase
{
    public override string? GroupName => "admin/users";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetUsers, "")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapGet(GetUserDetails, "{id}")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(UpdateUserStatus, "{id}/status")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(UpdateUserRole, "{id}/role")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public async Task<Ok<List<AdminUserSummaryDto>>> GetUsers(
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);

        var result = new List<AdminUserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
            var role = isAdmin ? ApiRoles.Admin : ApiRoles.User;

            var isActive = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow;

            result.Add(new AdminUserSummaryDto(
                user.Id,
                user.Email,
                user.UserName,
                role,
                isActive,
                user.CreatedAt,
                user.LastLoginAt));
        }

        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<AdminUserDetailsDto>, NotFound>> GetUserDetails(
        UserManager<ApplicationUser> userManager,
        string id,
        CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var isAdmin = roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
        var role = isAdmin ? ApiRoles.Admin : ApiRoles.User;
        var isActive = !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow;

        var dto = new AdminUserDetailsDto(
            user.Id,
            user.Email,
            user.UserName,
            role,
            isActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.LastLoginAt);

        return TypedResults.Ok(dto);
    }

    public async Task<Results<NoContent, NotFound>> UpdateUserStatus(
        UserManager<ApplicationUser> userManager,
        string id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (request.IsActive)
        {
            user.LockoutEnd = null;
        }
        else
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }

        await userManager.UpdateAsync(user);

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, NotFound, BadRequest<string>>> UpdateUserRole(
        UserManager<ApplicationUser> userManager,
        string id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRole = request.Role?.Trim();
        if (!string.Equals(normalizedRole, ApiRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, ApiRoles.User, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest("Role must be either 'Admin' or 'User'.");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var isAdminTarget = string.Equals(normalizedRole, ApiRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var currentlyInAdminRole = await userManager.IsInRoleAsync(user, Roles.Administrator);

        if (isAdminTarget && !currentlyInAdminRole)
        {
            await userManager.AddToRoleAsync(user, Roles.Administrator);
        }
        else if (!isAdminTarget && currentlyInAdminRole)
        {
            await userManager.RemoveFromRoleAsync(user, Roles.Administrator);
        }

        return TypedResults.NoContent();
    }
}

