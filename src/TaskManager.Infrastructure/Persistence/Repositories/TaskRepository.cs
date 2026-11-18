using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Domain.Tasks;
using TaskManager.Infrastructure.Persistence.Extensions;

namespace TaskManager.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(AppDbContext context) : ITaskRepository
{
    public Task<PagedResult<TaskItem>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken)
    {
        var query = context.Tasks.AsNoTracking();

        if (filter.Status.HasValue)
            query = query.Where(task => task.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Assignee))
        {
            var assignee = filter.Assignee.Trim().ToLowerInvariant();
            query = query.Where(task => task.Assignee.ToLower() == assignee);
        }

        return query
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .ToPagedResultAsync(filter, cancellationToken);
    }

    public Task<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => context.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        context.Tasks.Add(task);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        context.Tasks.Update(task);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        context.Tasks.Remove(task);
        await context.SaveChangesAsync(cancellationToken);
    }
}
