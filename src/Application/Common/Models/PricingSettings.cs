namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for pricing calculations.
/// These settings can be adjusted in appsettings.json without code changes.
/// </summary>
public class PricingSettings
{
    /// <summary>
    /// The minimum number of members required for a group to qualify for 50/50 payment split.
    /// Defaults to 10 if not configured.
    /// </summary>
    public int LargeGroupThreshold { get; set; } = 10;
}
