using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using TaskFlow.Api.Constants;
using TaskFlow.Api.Errors;

namespace TaskFlow.Tests.Integration;

public class RateLimitingTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    [Fact]
    public async Task RequestsOverTheLimit_Return429WithCodeAndRetryAfter()
    {
        var client = CreateClientWithLimitOfTwo();

        await client.GetAsync("/api/tasks");
        await client.GetAsync("/api/tasks");
        var rejected = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
        Assert.Equal("TOO_MANY_REQUESTS", (await rejected.Content.ReadFromJsonAsync<ErrorResponse>())!.Code);
    }

    [Fact]
    public async Task HealthProbes_AreNotRateLimited()
    {
        var client = CreateClientWithLimitOfTwo();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.GetAsync(HealthEndpoints.Live);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private HttpClient CreateClientWithLimitOfTwo()
        => factory
            .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitLimit", "2"))
            .CreateClient();
}
