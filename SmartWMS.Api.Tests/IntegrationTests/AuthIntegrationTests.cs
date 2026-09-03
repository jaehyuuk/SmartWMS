using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartWMS.Api.Data;
using SmartWMS.Api.Tests.Helpers;
using SmartWMS.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartWMS.Api.Tests.IntegrationTests;

public class AuthIntegrationTests
{
    [Fact]
    public async Task Register_Should_Return_Success()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var request = new {
            UserId = "testuser",
            Password = "Test1234!",
            Name = "테스트사용자"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == "testuser");

        Assert.NotNull(user);
        Assert.Equal("테스트사용자", user.Name);
        Assert.Equal("USER", user.Role);
        Assert.True(user.IsActive);
        Assert.NotEqual("Test1234!", user.PasswordHash);
    }

    [Fact]
    public async Task Login_Should_Return_AccessToken()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var registerRequest = new {
            UserId = "testuser",
            Password = "Test1234!",
            Name = "테스트사용자"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

        var loginRequest = new {
            UserId = "testuser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var content = await loginResponse.Content
            .ReadAsStringAsync();

        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.Contains("accessToken", content);
    }

    [Fact]
    public async Task Me_Should_Return_CurrentUser_When_Token_Is_Valid()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var registerRequest = new {
            UserId = "testuser",
            Password = "Test1234!",
            Name = "테스트사용자"
        };

        await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        var loginRequest = new {
            UserId = "testuser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        var loginContent = await loginResponse.Content
            .ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(loginContent);

        var accessToken = jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("accessToken")
            .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var meResponse = await client.GetAsync(
            "/api/Auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var meContent = await meResponse.Content
            .ReadAsStringAsync();

        Assert.Contains(
            "testuser",
            meContent);

        Assert.Contains(
            "테스트사용자",
            meContent);
    }

    [Fact]
    public async Task AdminApi_Should_Return_Forbidden_When_UserRole_Is_User()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var registerRequest = new {
            UserId = "testuser",
            Password = "Test1234!",
            Name = "테스트사용자"
        };

        await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        var loginRequest = new {
            UserId = "testuser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        var loginContent = await loginResponse.Content
            .ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(loginContent);

        var accessToken = jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("accessToken")
            .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/Users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminApi_Should_Return_Ok_When_UserRole_Is_Admin()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var registerRequest = new {
            UserId = "adminuser",
            Password = "Test1234!",
            Name = "관리자"
        };

        await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .FirstAsync(x => x.UserId == "adminuser");

            user.Role = "ADMIN";

            await dbContext.SaveChangesAsync();
        }

        var loginRequest = new {
            UserId = "adminuser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        var loginContent = await loginResponse.Content
            .ReadAsStringAsync();

        using var jsonDocument =
            JsonDocument.Parse(loginContent);

        var accessToken = jsonDocument.RootElement
            .GetProperty("data")
            .GetProperty("accessToken")
            .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/Users");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Is_Inactive()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.RegisterAsync(
            client,
            "inactiveuser",
            "Test1234!",
            "비활성사용자");

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .FirstAsync(x => x.UserId == "inactiveuser");

            user.IsActive = false;

            await dbContext.SaveChangesAsync();
        }

        var loginRequest = new {
            UserId = "inactiveuser",
            Password = "Test1234!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Should_Update_User_Role()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        // 관리자 생성 + 로그인
        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        // 일반 사용자 생성
        await AuthTestHelper.RegisterAsync(
            client,
            "normaluser",
            "Test1234!",
            "일반사용자");

        int userId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.UserId == "normaluser");

            userId = user.Id;

            Assert.Equal(
                "USER",
                user.Role);
        }

        var roleRequest = new {
            Role = "ADMIN"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Users/{userId}/role",
            roleRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.Id == userId);

            Assert.Equal(
                "ADMIN",
                user.Role);
        }
    }

    [Fact]
    public async Task Admin_Should_Deactivate_User_And_Block_Login()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        // 관리자 생성 + 로그인
        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        // 일반 사용자 생성
        await AuthTestHelper.RegisterAsync(
            client,
            "normaluser",
            "Test1234!",
            "일반사용자");

        int userId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.UserId == "normaluser");

            userId = user.Id;

            Assert.True(user.IsActive);
        }

        // 계정 비활성화
        var activeRequest = new {
            IsActive = false
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Users/{userId}/active",
            activeRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        // DB 상태 확인
        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.Id == userId);

            Assert.False(user.IsActive);
        }

        // 비활성 사용자는 로그인 실패해야 함
        var loginRequest = new {
            UserId = "normaluser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_Should_Not_Deactivate_Self()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        // 관리자 생성 + 로그인
        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        int adminId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var admin = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.UserId == "adminuser");

            adminId = admin.Id;

            Assert.True(admin.IsActive);
            Assert.Equal("ADMIN", admin.Role);
        }

        var request = new {
            IsActive = false
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Users/{adminId}/active",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        // DB 값도 그대로인지 확인
        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var admin = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.Id == adminId);

            Assert.True(admin.IsActive);
            Assert.Equal("ADMIN", admin.Role);
        }
    }

    [Fact]
    public async Task Admin_Should_Not_Demote_Self()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        int adminId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var admin = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.UserId == "adminuser");

            adminId = admin.Id;

            Assert.Equal(
                "ADMIN",
                admin.Role);
        }

        var request = new {
            Role = "USER"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Users/{adminId}/role",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var admin = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.Id == adminId);

            Assert.Equal(
                "ADMIN",
                admin.Role);

            Assert.True(
                admin.IsActive);
        }
    }
    [Fact]
    public async Task Register_Should_Return_Conflict_When_UserId_Is_Duplicated()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var request = new {
            UserId = "testuser",
            Password = "Test1234!",
            Name = "테스트사용자"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var userCount = await dbContext.Users
            .AsNoTracking()
            .CountAsync(x => x.UserId == "testuser");

        Assert.Equal(
            1,
            userCount);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Password_Is_Wrong()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.RegisterAsync(
            client,
            "testuser",
            "Test1234!",
            "테스트사용자");

        var loginRequest = new {
            UserId = "testuser",
            Password = "WrongPassword!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var content = await response.Content
            .ReadAsStringAsync();

        Assert.DoesNotContain(
            "accessToken",
            content);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var loginRequest = new {
            UserId = "unknownuser",
            Password = "Test1234!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var content = await response.Content
            .ReadAsStringAsync();

        Assert.DoesNotContain(
            "accessToken",
            content);
    }

    [Fact]
    public async Task Admin_Should_Return_NotFound_When_Updating_Role_Of_Unknown_User()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        var request = new {
            Role = "ADMIN"
        };

        var response = await client.PutAsJsonAsync(
            "/api/Users/999999/role",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Should_Return_NotFound_When_Updating_Active_Of_Unknown_User()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        var request = new {
            IsActive = false
        };

        var response = await client.PutAsJsonAsync(
            "/api/Users/999999/active",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Should_Return_BadRequest_When_Role_Is_Invalid()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        await AuthTestHelper.RegisterAsync(
            client,
            "normaluser",
            "Test1234!",
            "일반사용자");

        int userId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstAsync(x => x.UserId == "normaluser");

            userId = user.Id;
        }

        var request = new {
            Role = "MASTER"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/Users/{userId}/role",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();

        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var savedUser = await verifyDbContext.Users
            .AsNoTracking()
            .FirstAsync(x => x.Id == userId);

        Assert.Equal(
            "USER",
            savedUser.Role);
    }

    [Fact]
    public async Task UsersApi_Should_Return_Unauthorized_When_Token_Is_Missing()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}