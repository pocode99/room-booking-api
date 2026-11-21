using System.Net;
using Xunit;

namespace RoomBooking.Tests.Integration;

[Collection("Api")]
public class HealthIntegrationTests
{
    private readonly HttpClient _client;

    public HealthIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_WithoutAToken_ReturnHealthy(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}