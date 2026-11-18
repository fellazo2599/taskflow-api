using TaskManager.Application.Tasks.Models.ViewModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Adapters;

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
