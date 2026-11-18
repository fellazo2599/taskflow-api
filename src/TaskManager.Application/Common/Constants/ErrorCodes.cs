namespace TaskManager.Application.Common.Constants;

// Stable codes returned to clients as { "code": "..." }; clients translate them.
public static class ErrorCodes
{
    public const string TaskNotFound = "TASK_NOT_FOUND";
    public const string InvalidRequest = "REQUEST_INVALID";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string ServerError = "SERVER_ERROR";
}
