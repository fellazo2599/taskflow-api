using FluentValidation;
using TaskFlow.Application.Common.Validators;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class FilterTasksRmValidator : AbstractValidator<FilterTasksRm>
{
    public FilterTasksRmValidator()
    {
        Include(new PaginationRmValidator());

        // Query binding accepts numeric values such as status=7, so the enum range is checked here.
        RuleFor(filter => filter.Status).IsInEnum();
        RuleFor(filter => filter.Assignee).MaximumLength(TaskItemConstraints.AssigneeMaxLength);
    }
}
