namespace TaskManager.Api.Settings;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Default";

    // Empty means no cross-origin access: origins must be listed explicitly per environment.
    public string[] AllowedOrigins { get; init; } = [];
}
