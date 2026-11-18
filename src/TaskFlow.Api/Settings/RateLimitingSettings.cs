namespace TaskFlow.Api.Settings;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
}
