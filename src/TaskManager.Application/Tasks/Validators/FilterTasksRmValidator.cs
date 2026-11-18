using FluentValidation;
using TaskManager.Application.Common.Validators;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Validators;

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
