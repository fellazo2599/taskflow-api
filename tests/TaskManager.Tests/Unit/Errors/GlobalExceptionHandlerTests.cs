using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManager.Api.Errors;
using TaskManager.Application.Common.Constants;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Tests.Unit.Errors;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler = new(NullLogger<GlobalExceptionHandler>.Instance);

    public static TheoryData<Exception, int, string> Cases => new()
    {
        { new ValidationException([new ValidationFailure("Title", "m") { ErrorCode = "NotEmptyValidator" }]), 400, "TITLE_REQUIRED" },
        { new NotFoundException(ErrorCodes.TaskNotFound), 404, ErrorCodes.TaskNotFound },
        { new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge), 413, ErrorCodes.PayloadTooLarge },
        { new BadHttpRequestException("Unexpected end of request content."), 400, ErrorCodes.InvalidRequest },
        { new InvalidOperationException("boom"), 500, ErrorCodes.ServerError }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task MapsExceptionToStatusAndCode(Exception exception, int expectedStatus, string expectedCode)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(expectedCode, error!.Code);
    }
}
