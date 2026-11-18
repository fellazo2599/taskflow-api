using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Interfaces;

public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken);
    Task<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
