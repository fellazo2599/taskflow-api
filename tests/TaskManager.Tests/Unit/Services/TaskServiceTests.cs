using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManager.Application.Common.Constants;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks.Models.RequestModels;
using TaskManager.Application.Tasks.Services;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Domain.Tasks;
using TaskManager.Tests.Support;

namespace TaskManager.Tests.Unit.Services;

public class TaskServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    private readonly FakeTaskRepository _repository = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        _service = new TaskService(
            _repository,
            new FilterTasksRmValidator(),
            new CreateTaskRmValidator(timeProvider),
            new UpdateTaskRmValidator(),
            NullLogger<TaskService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithoutStatus_DefaultsToPendingAndTrimsInput()
    {
        var request = new CreateTaskRm
        {
            Title = "  Prepare demo  ",
            Description = "   ",
            Assignee = " Ana ",
            DueDate = Today.AddDays(3)
        };

        var task = await _service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(TaskState.Pending, task.Status);
        Assert.Equal("Prepare demo", task.Title);
        Assert.Equal("Ana", task.Assignee);
        Assert.Null(task.Description);
        Assert.Single(_repository.Tasks);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsAndDoesNotPersist()
    {
        var request = new CreateTaskRm { Title = "", Assignee = "Ana", DueDate = Today };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request, CancellationToken.None));
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task GetByIdAsync_MissingTask_ThrowsNotFoundWithTaskCode()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(99, CancellationToken.None));

        Assert.Equal(ErrorCodes.TaskNotFound, exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_AllowsPastDueDateAndAppliesChanges()
    {
        var created = await _service.CreateAsync(
            new CreateTaskRm { Title = "Old", Assignee = "Ana", DueDate = Today }, CancellationToken.None);

        var updated = await _service.UpdateAsync(created.Id, new UpdateTaskRm
        {
            Title = "New",
            Assignee = "Bob",
            Status = TaskState.Done,
            DueDate = Today.AddDays(-10)
        }, CancellationToken.None);

        Assert.Equal("New", updated.Title);
        Assert.Equal("Bob", updated.Assignee);
        Assert.Equal(TaskState.Done, updated.Status);
        Assert.Equal(Today.AddDays(-10), updated.DueDate);
    }

    [Fact]
    public async Task DeleteAsync_MissingTask_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99, CancellationToken.None));
    }
}
