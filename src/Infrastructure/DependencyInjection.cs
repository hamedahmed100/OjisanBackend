using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Groups.Common;
using OjisanBackend.Domain.Constants;
using OjisanBackend.Infrastructure.Data;
using OjisanBackend.Infrastructure.Data.Interceptors;
using OjisanBackend.Infrastructure.ExternalServices;
using OjisanBackend.Infrastructure.Identity;
using OjisanBackend.Infrastructure.Notifications;
using OjisanBackend.Infrastructure.Payments;
using OjisanBackend.Infrastructure.Services;
using OjisanBackend.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("OjisanBackendDb");
        Guard.Against.Null(connectionString, message: "Connection string 'OjisanBackendDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });


        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromDays(7);
                options.RefreshTokenExpiration = TimeSpan.FromDays(30);
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IUserLookupService, UserLookupService>();
        builder.Services.AddSingleton<IInviteCodeService, InviteCodeService>();
        builder.Services.AddScoped<IImageUploadService, LocalImageUploadService>();
        builder.Services.AddScoped<IGroupPricingService, GroupPricingService>();

        // Register HttpClient for Fatorah payment service
        builder.Services.AddHttpClient(nameof(FatorahPaymentService));

        // Register HttpClient for Trello service
        builder.Services.AddHttpClient(nameof(TrelloService));

        // Register HttpClient for OTO shipping service
        builder.Services.AddHttpClient(nameof(OtoShippingService));

        // Register HttpClient for WhatsApp service
        builder.Services.AddHttpClient(nameof(WhatsAppService));

        // Register payment service
        builder.Services.AddScoped<IPaymentService, FatorahPaymentService>();

        // Register external services
        builder.Services.AddScoped<ITrelloService, TrelloService>();
        builder.Services.AddScoped<IShippingService, OtoShippingService>();

        // Register notification services
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator)));
    }
}
