using System.Net;
using SmartWMS.Api.Tests.Infrastructure;

namespace SmartWMS.Api.Tests.IntegrationTests;

public class HealthCheckIntegrationTests {
    [Fact]
    public async Task Health_Should_Return_Ok() {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content = await response.Content
            .ReadAsStringAsync();

        Assert.Equal(
            "Healthy",
            content);
    }
}