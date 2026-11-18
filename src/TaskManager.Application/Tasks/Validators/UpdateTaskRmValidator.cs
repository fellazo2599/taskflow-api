using FluentValidation;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Validators;

// Unlike creation, a past due date is allowed so overdue tasks can still be edited.
public sealed class UpdateTaskRmValidator : AbstractValidator<UpdateTaskRm>
{
    public UpdateTaskRmValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(task => task.Title).NotEmpty().MaximumLength(TaskItemConstraints.TitleMaxLength);
        RuleFor(task => task.Description).MaximumLength(TaskItemConstraints.DescriptionMaxLength);
        RuleFor(task => task.Status).NotNull().IsInEnum();
        RuleFor(task => task.Assignee).NotEmpty().MaximumLength(TaskItemConstraints.AssigneeMaxLength);
        RuleFor(task => task.DueDate).NotNull();
    }
}
