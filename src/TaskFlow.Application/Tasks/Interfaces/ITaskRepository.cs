using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Interfaces;

public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken);
    Task<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
