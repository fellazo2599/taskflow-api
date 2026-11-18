using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Errors;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tasks.Interfaces;
using TaskFlow.Application.Tasks.Models.RequestModels;
using TaskFlow.Application.Tasks.Models.ViewModels;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskVm>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] FilterTasksRm filter, CancellationToken cancellationToken)
        => Ok(await taskService.ListAsync(filter, cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType<TaskVm>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        => Ok(await taskService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<TaskVm>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRm request, CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:long}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<TaskVm>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTaskRm request, CancellationToken cancellationToken)
        => Ok(await taskService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
