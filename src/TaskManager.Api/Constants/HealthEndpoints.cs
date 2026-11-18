namespace TaskManager.Api.Constants;

public static class HealthEndpoints
{
    public const string BasePath = "/health";
    public const string Live = BasePath + "/live";
    public const string Ready = BasePath + "/ready";
}
