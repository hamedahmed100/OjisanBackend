using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Models;
using OjisanBackend.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Configure settings from appsettings.json
builder.Services.Configure<PricingSettings>(builder.Configuration.GetSection("PricingSettings"));
builder.Services.Configure<FatorahSettings>(builder.Configuration.GetSection("FatorahSettings"));
builder.Services.Configure<TrelloSettings>(builder.Configuration.GetSection("TrelloSettings"));
builder.Services.Configure<OtoSettings>(builder.Configuration.GetSection("OtoSettings"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));

// builder Configuration
var app = builder.Build();

// Auto-migrate and seed on startup (Docker/CI-CD: every deployment applies migrations + ensures admin/seed data)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    await initialiser.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHealthChecks("/health");
app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();

app.UseOpenApi(); // Generate OpenAPI from endpoints at runtime

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/swagger/v1/swagger.json";
});


app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

app.Run();

public partial class Program { }
