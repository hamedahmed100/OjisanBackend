using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Users.Commands.UpdateUserAddress;
using OjisanBackend.Domain.Constants;
using OjisanBackend.Infrastructure.Identity;

namespace OjisanBackend.Web.Endpoints;

public record SignInRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password);

public record RegisterSuccessResponse(string Message);

/// <summary>Address details for the current user. Returns empty strings when no address is set.</summary>
public record UserAddressResponse(string Street, string City, string District, string PostalCode, string PhoneNumber);

/// <summary>Current user info for the frontend. Role is either Admin or User. Group leader/member are per-group (use group APIs).</summary>
public record CurrentUserResponse(string Id, string? UserName, string? Email, string Role, UserAddressResponse Address);

/// <summary>Maps Identity roles to the single API role: Admin or User.</summary>
internal static class UserRoleMapping
{
    public static string ToApiRole(IReadOnlyList<string>? identityRoles)
    {
        if (identityRoles != null && identityRoles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase))
            return ApiRoles.Admin;
        return ApiRoles.User;
    }
}

public class Users : EndpointGroupBase
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // Custom registration: returns 201 Created. Uses /signup to avoid duplicate route with MapIdentityApi's /register (NSwag fails on duplicate).
        groupBuilder.MapPost("/signup", async Task<Results<Created<RegisterSuccessResponse>, ProblemHttpResult>>
            (RegisterRequest request, HttpContext context, IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var userStore = sp.GetRequiredService<IUserStore<ApplicationUser>>();
            var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
            var email = request.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email) || !EmailValidator.IsValid(email))
                return TypedResults.Problem("A valid email is required.", statusCode: StatusCodes.Status400BadRequest);

            var user = new ApplicationUser();
            await userStore.SetUserNameAsync(user, email, CancellationToken.None);
            await emailStore.SetEmailAsync(user, email, CancellationToken.None);
            var result = await userManager.CreateAsync(user, request.Password ?? string.Empty);

            if (!result.Succeeded)
                return TypedResults.Problem(
                    detail: string.Join(" ", result.Errors.Select(e => e.Description)),
                    statusCode: StatusCodes.Status400BadRequest);

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            // Optionally send confirmation email (if IEmailSender is registered). Link uses userId + email only (no code).
            var emailSender = sp.GetService<IEmailSender<ApplicationUser>>();
            if (emailSender != null)
            {
                var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}/api/Users/confirm-email?userId={Uri.EscapeDataString(user.Id)}&email={Uri.EscapeDataString(email)}";
                await emailSender.SendConfirmationLinkAsync(user, email, callbackUrl);
            }

            return TypedResults.Created("/api/Users/signup", new RegisterSuccessResponse("Created successfully!"));
        })
            .AllowAnonymous()
            .WithName("SignUp")
            .WithSummary("Register a new user. Returns 201 Created with success message. Prefer this over POST /register.")
            .Produces<RegisterSuccessResponse>(201)
            .ProducesProblem(400);

        // Simple confirm email by userId + email only (no token/code). Uses /confirm-email to avoid duplicate with MapIdentityApi's /confirmEmail.
        groupBuilder.MapGet("/confirm-email", async Task<Results<ContentHttpResult, UnauthorizedHttpResult>>
            (string userId, string email, UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                return TypedResults.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user == null || !string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
                return TypedResults.Unauthorized();

            if (user.EmailConfirmed)
                return TypedResults.Content("Email is already confirmed.", "text/plain");

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            return TypedResults.Content("Thank you for confirming your email.", "text/plain");
        })
            .AllowAnonymous()
            .WithName("ConfirmEmailByUser")
            .WithSummary("Confirm email with userId and email only (no code). Sets EmailConfirmed = true in the database.")
            .Produces(200, contentType: "text/plain")
            .Produces(401);

        groupBuilder.MapIdentityApi<ApplicationUser>();

        groupBuilder.MapPost("/signin", async Task<Results<EmptyHttpResult, ProblemHttpResult>>
            (SignInRequest request, HttpContext context, IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = sp.GetRequiredService<SignInManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return TypedResults.Problem("Invalid login attempt.", statusCode: StatusCodes.Status401Unauthorized);

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
                return TypedResults.Problem("Invalid login attempt.", statusCode: StatusCodes.Status401Unauthorized);

            return TypedResults.Empty;
        })
            .AllowAnonymous()
            .WithName("SignIn")
            .WithSummary("Sign in with email and password only.")
            .Produces(200)
            .ProducesProblem(401);

        groupBuilder.MapGet("/me", async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>>
            (IUser currentUser, UserManager<ApplicationUser> userManager, IApplicationDbContext dbContext) =>
        {
            if (string.IsNullOrEmpty(currentUser.Id))
                return TypedResults.Unauthorized();

            var user = await userManager.FindByIdAsync(currentUser.Id);
            var userName = user?.UserName;
            var email = user?.Email;
            var role = UserRoleMapping.ToApiRole(currentUser.Roles);

            var address = await dbContext.UserAddresses
                .FirstOrDefaultAsync(a => a.UserId == currentUser.Id, CancellationToken.None);
            var addressResponse = address is null
                ? new UserAddressResponse(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
                : new UserAddressResponse(
                    address.Street ?? string.Empty,
                    address.City ?? string.Empty,
                    address.District ?? string.Empty,
                    address.PostalCode ?? string.Empty,
                    address.PhoneNumber ?? string.Empty);

            return TypedResults.Ok(new CurrentUserResponse(currentUser.Id, userName, email, role, addressResponse));
        })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Get current user id, userName, email, role and address. Address fields are empty strings when not set.")
            .Produces<CurrentUserResponse>(200)
            .Produces(401);

        groupBuilder.MapPut(UpdateUserAddress, "address")
            .RequireAuthorization();
    }

    public async Task<NoContent> UpdateUserAddress(ISender sender, UpdateUserAddressCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
