using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskFlow.Api.Errors;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Tasks.Models.ViewModels;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Tests.Integration;

public class TasksApiTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private const string TasksUrl = "/api/tasks";

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_ValidTask_Returns201WithLocationAndPendingStatus()
    {
        var response = await _client.PostAsJsonAsync(TasksUrl,
            new { title = "Write README", assignee = "Ana", dueDate = Today.AddDays(7) }, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await ReadAsync<TaskVm>(response);
        Assert.Equal(TaskState.Pending, task.Status);
        Assert.Equal($"{TasksUrl}/{task.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Post_BlankTitle_Returns400WithFieldCode()
    {
        var response = await _client.PostAsJsonAsync(TasksUrl,
            new { title = " ", assignee = "Ana", dueDate = Today.AddDays(1) }, JsonOptions);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "TITLE_REQUIRED");
    }

    [Theory]
    [InlineData("\"Archived\"")]
    [InlineData("1")]
    public async Task Post_UnknownOrNumericStatus_Returns400WithStatusInvalid(string status)
    {
        var body = $$"""{ "title": "Task", "assignee": "Ana", "dueDate": "{{Today.AddDays(1):yyyy-MM-dd}}", "status": {{status}} }""";

        var response = await _client.PostAsync(TasksUrl, new StringContent(body, Encoding.UTF8, "application/json"));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "STATUS_INVALID");
    }

    [Fact]
    public async Task GetById_MissingTask_Returns404WithTaskNotFound()
    {
        var response = await _client.GetAsync($"{TasksUrl}/987654");

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "TASK_NOT_FOUND");
    }

    [Fact]
    public async Task Put_ExistingTask_Returns200WithChanges()
    {
        var created = await CreateTaskAsync("Draft", "Ana", Today.AddDays(2));

        var response = await _client.PutAsJsonAsync($"{TasksUrl}/{created.Id}",
            new { title = "Final", assignee = "Bob", status = "Done", dueDate = Today.AddDays(5) }, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadAsync<TaskVm>(response);
        Assert.Equal("Final", updated.Title);
        Assert.Equal(TaskState.Done, updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204AndTaskIsGone()
    {
        var created = await CreateTaskAsync("Disposable", "Ana", Today.AddDays(1));

        var deleteResponse = await _client.DeleteAsync($"{TasksUrl}/{created.Id}");
        var getResponse = await _client.GetAsync($"{TasksUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        await AssertErrorAsync(getResponse, HttpStatusCode.NotFound, "TASK_NOT_FOUND");
    }

    [Fact]
    public async Task List_FiltersByStatusAndAssigneeWithPagination()
    {
        var assignee = $"assignee-{Guid.NewGuid():N}";
        await CreateTaskAsync("First pending", assignee, Today.AddDays(1));
        await CreateTaskAsync("Second pending", assignee, Today.AddDays(2));
        await CreateTaskAsync("Done one", assignee, Today.AddDays(3), TaskState.Done);
        await CreateTaskAsync("Someone else", "other-assignee", Today.AddDays(1));

        var response = await _client.GetAsync(
            $"{TasksUrl}?assignee={assignee.ToUpperInvariant()}&status=Pending&page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResult<TaskVm>>(response);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        var task = Assert.Single(page.Items);
        Assert.Equal("Second pending", task.Title);
    }

    [Theory]
    [InlineData("pageSize=1000", "PAGE_SIZE_OUT_OF_RANGE")]
    [InlineData("page=2147483647", "PAGE_OUT_OF_RANGE")]
    public async Task List_PaginationOutOfRange_Returns400(string query, string code)
    {
        var response = await _client.GetAsync($"{TasksUrl}?{query}");

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, code);
    }

    [Fact]
    public async Task ApiResponses_IncludeSecurityHeaders()
    {
        var response = await _client.GetAsync($"{TasksUrl}/987654");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    private async Task<TaskVm> CreateTaskAsync(string title, string assignee, DateOnly dueDate, TaskState? status = null)
    {
        var response = await _client.PostAsJsonAsync(TasksUrl, new { title, assignee, dueDate, status }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<TaskVm>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await ReadAsync<ErrorResponse>(response)).Code);
    }
}
