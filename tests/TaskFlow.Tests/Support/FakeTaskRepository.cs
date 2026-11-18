using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tasks.Interfaces;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Tests.Support;

// In-memory repository for service tests; filtering and paging are covered by the integration tests.
internal sealed class FakeTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];
    private long _nextId = 1;

    public IReadOnlyList<TaskItem> Tasks => _tasks;

    public Task<PagedResult<TaskItem>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken)
        => Task.FromResult(new PagedResult<TaskItem>(_tasks.ToList(), filter.Page, filter.PageSize, _tasks.Count));

    public Task<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => Task.FromResult(_tasks.FirstOrDefault(task => task.Id == id));

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        task.Id = _nextId++;
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _tasks.Remove(task);
        return Task.CompletedTask;
    }
}
