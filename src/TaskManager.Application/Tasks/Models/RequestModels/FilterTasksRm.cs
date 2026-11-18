using TaskManager.Application.Common.Models;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Models.RequestModels;

public sealed class FilterTasksRm : PaginationRm
{
    public TaskState? Status { get; set; }

    // Exact match, case-insensitive.
    public string? Assignee { get; set; }
}
