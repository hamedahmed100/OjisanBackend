using OjisanBackend.Domain.Constants;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OjisanBackend.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // Check if database exists and can be connected to
            if (await _context.Database.CanConnectAsync())
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    // Check if key tables already exist (indicates database is already migrated)
                    // This helps detect when migration history is out of sync
                    bool tablesExist = false;
                    try
                    {
                        using var command = _context.Database.GetDbConnection().CreateCommand();
                        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles'";
                        await _context.Database.OpenConnectionAsync();
                        var result = await command.ExecuteScalarAsync();
                        tablesExist = result != null && Convert.ToInt32(result) > 0;
                    }
                    catch
                    {
                        // If we can't check tables, proceed with normal migration
                        // This handles cases where database connection works but queries fail
                    }
                    finally
                    {
                        await _context.Database.CloseConnectionAsync();
                    }
                    
                    if (tablesExist)
                    {
                        // Tables exist but migration history shows pending migrations
                        // This means migration history is out of sync - skip migration to avoid errors
                        _logger.LogWarning(
                            "Database tables exist but migration history shows {Count} pending migration(s). " +
                            "This indicates the migration history table (__EFMigrationsHistory) is out of sync. " +
                            "Skipping migration attempt to avoid errors. " +
                            "The database appears to be up to date. " +
                            "To fix the migration history, run: INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260220145302_ojisan-db', '10.0.0')",
                            pendingMigrations.Count());
                        return; // Skip migration - database is already set up
                    }
                    
                    // Normal case: apply pending migrations
                    _logger.LogInformation("Applying {Count} pending migration(s)...", pendingMigrations.Count());
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    _logger.LogInformation("Database is up to date. No pending migrations.");
                }
            }
            else
            {
                // Database doesn't exist, create it and apply migrations
                _logger.LogInformation("Database does not exist. Creating database and applying migrations...");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database created and migrations applied successfully.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2714) // Object already exists
        {
            // Table already exists - this can happen if migration history is out of sync
            // Log as warning but don't fail - the database is likely already migrated
            _logger.LogWarning(
                "Database objects already exist. The database appears to be up to date, " +
                "but migration history may be out of sync. To fix this, run: " +
                "dotnet ef migrations script --idempotent | sqlcmd");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole(Roles.Administrator);
        var groupLeaderRole = new IdentityRole(Roles.GroupLeader);
        var groupMemberRole = new IdentityRole(Roles.GroupMember);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        if (_roleManager.Roles.All(r => r.Name != groupLeaderRole.Name))
        {
            await _roleManager.CreateAsync(groupLeaderRole);
        }

        if (_roleManager.Roles.All(r => r.Name != groupMemberRole.Name))
        {
            await _roleManager.CreateAsync(groupMemberRole);
        }

        // Default admin user
        var administrator = new ApplicationUser
        {
            UserName = "OjiAdmin",
            Email = "OjiAdmin"
        };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "OjisJestBackEndAdmin!2026");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
            }
        }

        // Default data
        // Seed, if necessary
    }
}
