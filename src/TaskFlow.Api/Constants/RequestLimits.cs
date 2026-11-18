namespace TaskFlow.Api.Constants;

public static class RequestLimits
{
    // A task payload is a few KB at most; Kestrel's 30 MB default is far more than needed.
    public const long MaxBodySizeBytes = 1024 * 1024;
}
