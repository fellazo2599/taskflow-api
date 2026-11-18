namespace TaskFlow.Application.Common.Exceptions;

// Carries only the error code; the API layer decides the HTTP status.
public sealed class NotFoundException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
