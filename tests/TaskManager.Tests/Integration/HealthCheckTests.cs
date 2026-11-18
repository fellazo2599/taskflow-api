using System.Net;
using TaskManager.Api.Constants;

namespace TaskManager.Tests.Integration;

public class HealthCheckTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(HealthEndpoints.Live)]
    [InlineData(HealthEndpoints.Ready)]
    public async Task HealthEndpoints_ReportHealthy(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
