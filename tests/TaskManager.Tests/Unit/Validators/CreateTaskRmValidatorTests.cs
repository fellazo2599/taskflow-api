using FluentValidation.TestHelper;
using TaskManager.Application.Common.Constants;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Domain.Tasks;
using TaskManager.Tests.Support;

namespace TaskManager.Tests.Unit.Validators;

public class CreateTaskRmValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    private readonly CreateTaskRmValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(Request());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DueDateToday_IsAccepted()
    {
        var result = _validator.TestValidate(Request(dueDate: Today));

        result.ShouldNotHaveValidationErrorFor(request => request.DueDate);
    }

    [Fact]
    public void DueDateInThePast_FailsWithInPastCode()
    {
        var result = _validator.TestValidate(Request(dueDate: Today.AddDays(-1)));

        result.ShouldHaveValidationErrorFor(request => request.DueDate).WithErrorCode(FieldErrorCodes.InPast);
    }

    [Fact]
    public void MissingDueDate_IsRequired()
    {
        var result = _validator.TestValidate(new CreateTaskRm { Title = "Write tests", Assignee = "Ana" });

        result.ShouldHaveValidationErrorFor(request => request.DueDate).WithErrorCode("NotNullValidator");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTitle_IsRequired(string title)
    {
        var result = _validator.TestValidate(Request(title: title));

        result.ShouldHaveValidationErrorFor(request => request.Title).WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public void TitleOverMaxLength_IsTooLong()
    {
        var result = _validator.TestValidate(Request(title: new string('x', TaskItemConstraints.TitleMaxLength + 1)));

        result.ShouldHaveValidationErrorFor(request => request.Title).WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public void UndefinedStatus_IsInvalid()
    {
        var result = _validator.TestValidate(Request(status: (TaskState)42));

        result.ShouldHaveValidationErrorFor(request => request.Status);
    }

    private static CreateTaskRm Request(
        string title = "Write tests", string assignee = "Ana", DateOnly? dueDate = null, TaskState? status = null)
        => new()
        {
            Title = title,
            Assignee = assignee,
            DueDate = dueDate ?? Today.AddDays(7),
            Status = status
        };
}
