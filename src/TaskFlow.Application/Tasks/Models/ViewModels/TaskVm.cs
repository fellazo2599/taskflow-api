using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Models.ViewModels;

public sealed class TaskVm
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskState Status { get; init; }
    public string Assignee { get; init; } = string.Empty;
    public DateOnly DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
