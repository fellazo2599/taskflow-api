using TaskManager.Api.Middlewares;

namespace TaskManager.Tests.Integration;

public class CorrelationIdTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task WellFormedIncomingId_IsEchoedBack()
    {
        var response = await SendAsync("client-abc-123");

        Assert.Equal("client-abc-123", CorrelationId(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("contains spaces <and> markup")]
    public async Task MissingOrMalformedId_IsReplacedByGeneratedOne(string? incoming)
    {
        var response = await SendAsync(incoming);

        var correlationId = CorrelationId(response);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual(incoming, correlationId);
    }

    private Task<HttpResponseMessage> SendAsync(string? correlationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks/987654");
        if (correlationId is not null)
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        return _client.SendAsync(request);
    }

    private static string CorrelationId(HttpResponseMessage response)
        => response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
}
