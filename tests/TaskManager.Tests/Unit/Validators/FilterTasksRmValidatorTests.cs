using FluentValidation.TestHelper;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Domain.Tasks;

namespace TaskManager.Tests.Unit.Validators;

public class FilterTasksRmValidatorTests
{
    private readonly FilterTasksRmValidator _validator = new();

    [Fact]
    public void Defaults_AreValid()
    {
        _validator.TestValidate(new FilterTasksRm()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PageBelowOne_IsInvalid()
    {
        _validator.TestValidate(new FilterTasksRm { Page = 0 })
            .ShouldHaveValidationErrorFor(filter => filter.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSizeOutsideLimits_IsInvalid(int pageSize)
    {
        _validator.TestValidate(new FilterTasksRm { PageSize = pageSize })
            .ShouldHaveValidationErrorFor(filter => filter.PageSize);
    }

    [Fact]
    public void UndefinedStatus_IsInvalid()
    {
        _validator.TestValidate(new FilterTasksRm { Status = (TaskState)7 })
            .ShouldHaveValidationErrorFor(filter => filter.Status);
    }
}
