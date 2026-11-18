using TaskManager.Application.Common.Models;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Interfaces;

public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> ListAsync(PaginationRm pagination, CancellationToken cancellationToken);
    Task<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
