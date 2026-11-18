namespace TaskFlow.Infrastructure.HealthChecks;

public static class HealthCheckTags
{
    // Checks that must pass before the instance receives traffic (e.g. the database is reachable).
    public const string Ready = "ready";
}
