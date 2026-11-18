using FluentValidation;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Validators;

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
