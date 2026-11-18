using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using TaskManager.Api.Errors;

namespace TaskManager.Tests.Integration;

public class RateLimitingTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    [Fact]
    public async Task RequestsOverTheLimit_Return429WithCodeAndRetryAfter()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitLimit", "2"))
            .CreateClient();

        await client.GetAsync("/api/tasks");
        await client.GetAsync("/api/tasks");
        var rejected = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
        Assert.Equal("TOO_MANY_REQUESTS", (await rejected.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
    }
}
