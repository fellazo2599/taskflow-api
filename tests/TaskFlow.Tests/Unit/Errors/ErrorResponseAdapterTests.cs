using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Api.Errors.Adapters;
using TaskFlow.Application.Common.Constants;

namespace TaskFlow.Tests.Unit.Errors;

public class ErrorResponseAdapterTests
{
    [Theory]
    [InlineData("Title", "NotEmptyValidator", "TITLE_REQUIRED")]
    [InlineData("DueDate", FieldErrorCodes.InPast, "DUE_DATE_IN_PAST")]
    [InlineData("PageSize", "InclusiveBetweenValidator", "PAGE_SIZE_OUT_OF_RANGE")]
    [InlineData("Assignee", "MaximumLengthValidator", "ASSIGNEE_TOO_LONG")]
    [InlineData("Status", "EnumValidator", "STATUS_INVALID")]
    public void ValidationException_MapsFirstFailureToFieldRuleCode(string property, string errorCode, string expected)
    {
        var exception = new ValidationException(
        [
            new ValidationFailure(property, "message") { ErrorCode = errorCode },
            new ValidationFailure("Other", "ignored") { ErrorCode = "NotEmptyValidator" }
        ]);

        Assert.Equal(expected, exception.ToErrorResponse().Code);
    }

    [Theory]
    [InlineData("$.dueDate", "DUE_DATE_INVALID")]
    [InlineData("Status", "STATUS_INVALID")]
    [InlineData("$", ErrorCodes.InvalidRequest)]
    public void ModelState_MapsBindingErrorToCode(string key, string expected)
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(key, "binding failed");

        Assert.Equal(expected, modelState.ToErrorResponse().Code);
    }
}
