using SmartWMS.Api.Tests.Infrastructure;

namespace SmartWMS.Api.Tests.IntegrationTests;

public class SmokeTest
{
    [Fact]
    public async Task Api_Should_Start()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/swagger/index.html");

        Assert.NotNull(response);
    }
}