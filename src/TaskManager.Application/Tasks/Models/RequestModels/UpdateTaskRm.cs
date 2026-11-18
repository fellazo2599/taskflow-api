using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Models.RequestModels;

public sealed class UpdateTaskRm
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskState? Status { get; init; }
    public string Assignee { get; init; } = string.Empty;
    public DateOnly? DueDate { get; init; }
}
