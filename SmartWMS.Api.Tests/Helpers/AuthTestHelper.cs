using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartWMS.Api.Data;
using SmartWMS.Api.Tests.Infrastructure;

namespace SmartWMS.Api.Tests.Helpers;

public static class AuthTestHelper
{
    public static async Task RegisterAsync(
        HttpClient client,
        string userId,
        string password,
        string name)
    {

        var request = new {
            UserId = userId,
            Password = password,
            Name = name
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    public static async Task SetAdminRoleAsync(
        CustomWebApplicationFactory factory,
        string userId)
    {

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var user = await dbContext.Users
            .FirstAsync(x => x.UserId == userId);

        user.Role = "ADMIN";

        await dbContext.SaveChangesAsync();
    }

    public static async Task<string> LoginAsync(
        HttpClient client,
        string userId,
        string password)
    {

        var request = new {
            UserId = userId,
            Password = password
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var content = await response.Content
            .ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(content);

        var accessToken = jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("accessToken")
            .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        return accessToken!;
    }

    public static void SetBearerToken(
        HttpClient client,
        string accessToken)
    {

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
    }

    public static async Task<string> CreateAdminAndLoginAsync(
        CustomWebApplicationFactory factory,
        HttpClient client)
    {

        const string userId = "adminuser";
        const string password = "Test1234!";

        await RegisterAsync(
            client,
            userId,
            password,
            "관리자");

        await SetAdminRoleAsync(
            factory,
            userId);

        var accessToken = await LoginAsync(
            client,
            userId,
            password);

        SetBearerToken(
            client,
            accessToken);

        return accessToken;
    }
}