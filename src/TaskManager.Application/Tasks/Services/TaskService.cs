using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Constants;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Adapters;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Application.Tasks.Models.ViewModels;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Services;

public sealed class TaskService(
    ITaskRepository taskRepository,
    IValidator<PaginationRm> paginationValidator,
    IValidator<CreateTaskRm> createValidator,
    IValidator<UpdateTaskRm> updateValidator,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<PagedResult<TaskVm>> ListAsync(PaginationRm pagination, CancellationToken cancellationToken)
    {
        await paginationValidator.ValidateAndThrowAsync(pagination, cancellationToken);

        var tasks = await taskRepository.ListAsync(pagination, cancellationToken);
        return tasks.Map(task => task.ToVm());
    }

    public async Task<TaskVm> GetByIdAsync(long id, CancellationToken cancellationToken)
        => (await FindAsync(id, cancellationToken)).ToVm();

    public async Task<TaskVm> CreateAsync(CreateTaskRm request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = request.ToEntity();
        await taskRepository.AddAsync(task, cancellationToken);

        logger.LogInformation("Task {TaskId} created", task.Id);
        return task.ToVm();
    }

    public async Task<TaskVm> UpdateAsync(long id, UpdateTaskRm request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = await FindAsync(id, cancellationToken);
        request.ApplyTo(task);
        await taskRepository.UpdateAsync(task, cancellationToken);

        logger.LogInformation("Task {TaskId} updated", task.Id);
        return task.ToVm();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var task = await FindAsync(id, cancellationToken);
        await taskRepository.DeleteAsync(task, cancellationToken);

        logger.LogInformation("Task {TaskId} deleted", id);
    }

    private async Task<TaskItem> FindAsync(long id, CancellationToken cancellationToken)
        => await taskRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(ErrorCodes.TaskNotFound);
}
