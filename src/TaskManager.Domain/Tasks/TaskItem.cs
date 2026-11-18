using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

// Named TaskItem to avoid clashing with System.Threading.Tasks.Task.
public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState Status { get; set; }
    public string Assignee { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
}
