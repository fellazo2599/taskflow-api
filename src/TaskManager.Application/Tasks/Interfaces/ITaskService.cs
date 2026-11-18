using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Application.Tasks.Models.ViewModels;

namespace TaskManager.Application.Tasks.Interfaces;

public interface ITaskService
{
    Task<PagedResult<TaskVm>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken);
    Task<TaskVm> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<TaskVm> CreateAsync(CreateTaskRm request, CancellationToken cancellationToken);
    Task<TaskVm> UpdateAsync(long id, UpdateTaskRm request, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
