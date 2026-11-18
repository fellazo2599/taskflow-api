using TaskFlow.Application.Common.Models;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Models.RequestModels;

public sealed class FilterTasksRm : PaginationRm
{
    public TaskState? Status { get; set; }

    // Exact match, case-insensitive.
    public string? Assignee { get; set; }
}
