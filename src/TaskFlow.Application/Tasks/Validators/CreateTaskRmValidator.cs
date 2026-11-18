using FluentValidation;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class CreateTaskRmValidator : AbstractValidator<CreateTaskRm>
{
    public CreateTaskRmValidator(TimeProvider timeProvider)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(task => task.Title).NotEmpty().MaximumLength(TaskItemConstraints.TitleMaxLength);
        RuleFor(task => task.Description).MaximumLength(TaskItemConstraints.DescriptionMaxLength);
        RuleFor(task => task.Status).IsInEnum();
        RuleFor(task => task.Assignee).NotEmpty().MaximumLength(TaskItemConstraints.AssigneeMaxLength);
        RuleFor(task => task.DueDate)
            .NotNull()
            .Must(dueDate => dueDate >= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithErrorCode(FieldErrorCodes.InPast);
    }
}
