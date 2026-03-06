using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace OjisanBackend.Web.Infrastructure;

/// <summary>
/// In Development, after the app starts, fetches the runtime OpenAPI document and writes it to
/// wwwroot/api/specification.json so the static file and Swagger always reflect the latest endpoints.
/// </summary>
public class OpenApiSpecRefreshHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<OpenApiSpecRefreshHostedService> _logger;

    public OpenApiSpecRefreshHostedService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHostApplicationLifetime lifetime,
        ILogger<OpenApiSpecRefreshHostedService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _lifetime.ApplicationStarted.Register(() =>
        {
            _ = RefreshSpecAsync();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RefreshSpecAsync()
    {
        try
        {
            // Give the server a moment to be ready to serve requests
            await Task.Delay(1500);

            var baseUrl = _configuration["OpenApiSpecBaseUrl"]
                ?? _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogDebug("OpenApiSpecRefresh: No OpenApiSpecBaseUrl or ASPNETCORE_URLS; skipping static spec refresh.");
                return;
            }

            baseUrl = baseUrl.TrimEnd('/');
            var specUrl = $"{baseUrl}/swagger/v1/swagger.json";

            using var handler = new HttpClientHandler();
            if (specUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }
            using var http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(10);
            var json = await http.GetStringAsync(specUrl);

            var path = Path.Combine(_environment.WebRootPath ?? "wwwroot", "api", "specification.json");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("OpenApiSpecRefresh: Updated {Path} from runtime document.", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenApiSpecRefresh: Could not refresh specification.json (app may not be reachable at OpenApiSpecBaseUrl).");
        }
    }
}
