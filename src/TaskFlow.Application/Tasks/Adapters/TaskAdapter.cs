using TaskFlow.Application.Tasks.Models.ViewModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Adapters;

public static class TaskAdapter
{
    public static TaskVm ToVm(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Assignee = task.Assignee,
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
