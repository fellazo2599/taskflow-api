using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Constants;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tasks.Adapters;
using TaskFlow.Application.Tasks.Interfaces;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Application.Tasks.Models.ViewModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks.Services;

public sealed class TaskService(
    ITaskRepository taskRepository,
    IValidator<FilterTasksRm> filterValidator,
    IValidator<CreateTaskRm> createValidator,
    IValidator<UpdateTaskRm> updateValidator,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<PagedResult<TaskVm>> ListAsync(FilterTasksRm filter, CancellationToken cancellationToken)
    {
        await filterValidator.ValidateAndThrowAsync(filter, cancellationToken);

        var tasks = await taskRepository.ListAsync(filter, cancellationToken);
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
