using System.Net;
using TaskFlow.Api.Constants;

namespace TaskFlow.Tests.Integration;

public class HealthCheckTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
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
