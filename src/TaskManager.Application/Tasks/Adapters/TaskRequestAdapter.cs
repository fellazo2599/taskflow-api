using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Adapters;

// Expects requests that already passed validation.
public static class TaskRequestAdapter
{
    public static TaskItem ToEntity(this CreateTaskRm request) => new()
    {
        Title = request.Title.Trim(),
        Description = NormalizeDescription(request.Description),
        Status = request.Status ?? TaskState.Pending,
        Assignee = request.Assignee.Trim(),
        DueDate = request.DueDate!.Value
    };

    public static void ApplyTo(this UpdateTaskRm request, TaskItem task)
    {
        task.Title = request.Title.Trim();
        task.Description = NormalizeDescription(request.Description);
        task.Status = request.Status!.Value;
        task.Assignee = request.Assignee.Trim();
        task.DueDate = request.DueDate!.Value;
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
