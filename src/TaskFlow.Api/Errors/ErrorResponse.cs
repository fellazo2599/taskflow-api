namespace TaskFlow.Api.Errors;

// The only error body the API returns: { "code": "..." }.
public sealed record ErrorResponse(string Code);
