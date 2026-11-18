using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Common.Constants;

namespace TaskFlow.Api.Errors.Adapters;

// Builds FIELD_RULE codes (e.g. DUE_DATE_IN_PAST). Only the first error is reported.
public static partial class ErrorResponseAdapter
{
    public static ErrorResponse ToErrorResponse(this ValidationException exception)
    {
        var failure = exception.Errors.First();
        return new ErrorResponse($"{ToFieldCode(failure.PropertyName)}_{ToRuleCode(failure.ErrorCode)}");
    }

    // Model binding failures (malformed JSON, unknown enum value, wrong type) are keyed by the
    // JSON path ("$.dueDate") or by the query parameter name ("status").
    public static ErrorResponse ToErrorResponse(this ModelStateDictionary modelState)
    {
        var field = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => entry.Key.Split('.')[^1].TrimStart('$'))
            .FirstOrDefault(key => key.Length > 0);

        return new ErrorResponse(field is null
            ? ErrorCodes.InvalidRequest
            : $"{ToFieldCode(field)}_{FieldErrorCodes.Invalid}");
    }

    public static ErrorResponse ToErrorResponse(this BadHttpRequestException exception)
        => new(exception.StatusCode == StatusCodes.Status413PayloadTooLarge
            ? ErrorCodes.PayloadTooLarge
            : ErrorCodes.InvalidRequest);

    private static string ToFieldCode(string propertyName)
        => WordBoundary().Replace(propertyName, "_").ToUpperInvariant();

    // FluentValidation uses the validator's type name as the default error code.
    private static string ToRuleCode(string validatorErrorCode) => validatorErrorCode switch
    {
        nameof(NotNullValidator<,>) or nameof(NotEmptyValidator<,>) => FieldErrorCodes.Required,
        nameof(MaximumLengthValidator<>) => FieldErrorCodes.TooLong,
        nameof(InclusiveBetweenValidator<,>) => FieldErrorCodes.OutOfRange,
        FieldErrorCodes.InPast => FieldErrorCodes.InPast,
        _ => FieldErrorCodes.Invalid
    };

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
