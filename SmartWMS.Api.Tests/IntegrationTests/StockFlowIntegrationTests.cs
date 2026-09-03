using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartWMS.Api.Data;
using SmartWMS.Api.Models;
using SmartWMS.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartWMS.Api.Tests.Helpers;

namespace SmartWMS.Api.Tests.IntegrationTests;

public class StockFlowIntegrationTests
{
    [Fact]
    public async Task Stock_Flow_Should_Work_Correctly()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        await AuthTestHelper.CreateAdminAndLoginAsync(
            factory,
            client);

        var productRequest = new {
            Code = "P001",
            Name = "테스트상품"
        };

        var productResponse = await client.PostAsJsonAsync(
            "/api/Product",
            productRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            productResponse.StatusCode);

        int productId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "P001");

            Assert.NotNull(product);
            Assert.Equal("테스트상품", product.Name);
            Assert.Equal(0, product.StockQuantity);

            productId = product.Id;
        }

        var inboundRequest = new {
            ProductId = productId,
            Quantity = 10,
            Memo = "통합 테스트 입고"
        };

        var inboundResponse = await client.PostAsJsonAsync(
            "/api/Inbound",
            inboundRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            inboundResponse.StatusCode);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            Assert.Equal(
                10,
                product.StockQuantity);

            var inbound = await dbContext.Inbounds
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId);

            Assert.NotNull(inbound);
            Assert.Equal(10, inbound.Quantity);
            Assert.Equal("통합 테스트 입고", inbound.Memo);
        }

        var outboundRequest = new {
            ProductId = productId,
            Quantity = 3,
            Memo = "통합 테스트 출고"
        };

        var outboundResponse = await client.PostAsJsonAsync(
            "/api/Outbound",
            outboundRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            outboundResponse.StatusCode);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            Assert.Equal(
                7,
                product.StockQuantity);

            var outbound = await dbContext.Outbounds
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId);

            Assert.NotNull(outbound);
            Assert.Equal(3, outbound.Quantity);
            Assert.Equal("통합 테스트 출고", outbound.Memo);
        }

        var shortageRequest = new {
            ProductId = productId,
            Quantity = 10,
            Memo = "재고 부족 테스트"
        };

        var shortageResponse = await client.PostAsJsonAsync(
            "/api/Outbound",
            shortageRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            shortageResponse.StatusCode);

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            Assert.Equal(
                7,
                product.StockQuantity);

            var outboundCount = await dbContext.Outbounds
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductId == productId);

            Assert.Equal(
                1,
                outboundCount);
        }
    }

    [Fact]
    public async Task Concurrent_Stock_Update_Should_Throw_ConcurrencyException()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        int productId;

        // 테스트 상품 생성
        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = new Product {
                Code = "CON001",
                Name = "동시성테스트상품",
                StockQuantity = 10
            };

            dbContext.Products.Add(product);

            await dbContext.SaveChangesAsync();

            productId = product.Id;
        }

        // 서로 다른 요청처럼 별도의 Scope / DbContext 생성
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();

        var dbContext1 = scope1.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var dbContext2 = scope2.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var product1 = await dbContext1.Products
            .FirstAsync(x => x.Id == productId);

        var product2 = await dbContext2.Products
            .FirstAsync(x => x.Id == productId);

        // 둘 다 같은 시점의 재고와 RowVersion을 들고 있음
        Assert.Equal(10, product1.StockQuantity);
        Assert.Equal(10, product2.StockQuantity);

        product1.StockQuantity -= 7;
        product2.StockQuantity -= 7;

        // 첫 번째 요청 저장 성공
        await dbContext1.SaveChangesAsync();

        // 두 번째 요청은 이전 RowVersion으로 저장하므로 충돌
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await dbContext2.SaveChangesAsync());

        // 실제 DB 재고 확인
        using var verifyScope = factory.Services.CreateScope();

        var verifyDbContext = verifyScope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        var savedProduct = await verifyDbContext.Products
            .AsNoTracking()
            .FirstAsync(x => x.Id == productId);

        Assert.Equal(3, savedProduct.StockQuantity);
    }

    [Fact]
    public async Task Concurrent_Outbound_Requests_Should_Allow_Only_One()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        await factory.ResetDatabaseAsync();

        var client = factory.CreateClient();

        // 1. 관리자 계정 생성
        var registerRequest = new {
            UserId = "adminuser",
            Password = "Test1234!",
            Name = "관리자"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

        // 2. ADMIN 권한 부여
        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var user = await dbContext.Users
                .FirstAsync(x => x.UserId == "adminuser");

            user.Role = "ADMIN";

            await dbContext.SaveChangesAsync();
        }

        // 3. 로그인
        var loginRequest = new {
            UserId = "adminuser",
            Password = "Test1234!"
        };

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

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

        // 4. 상품 등록
        var productRequest = new {
            Code = "CON001",
            Name = "동시출고테스트상품"
        };

        var productResponse = await client.PostAsJsonAsync(
            "/api/Product",
            productRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            productResponse.StatusCode);

        int productId;

        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Code == "CON001");

            productId = product.Id;
        }

        // 5. 재고 10개 입고
        var inboundRequest = new {
            ProductId = productId,
            Quantity = 10,
            Memo = "동시성 테스트 입고"
        };

        var inboundResponse = await client.PostAsJsonAsync(
            "/api/Inbound",
            inboundRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            inboundResponse.StatusCode);

        // 6. 동일 상품 7개 출고 요청 2개 준비
        var outboundRequest1 = new {
            ProductId = productId,
            Quantity = 7,
            Memo = "동시 출고 1"
        };

        var outboundRequest2 = new {
            ProductId = productId,
            Quantity = 7,
            Memo = "동시 출고 2"
        };

        // 7. 동시에 HTTP 요청
        var task1 = client.PostAsJsonAsync(
            "/api/Outbound",
            outboundRequest1);

        var task2 = client.PostAsJsonAsync(
            "/api/Outbound",
            outboundRequest2);

        var responses = await Task.WhenAll(
            task1,
            task2);

        // 8. 하나는 성공, 하나는 Conflict여야 함
        Assert.Single(
            responses,
            x => x.StatusCode == HttpStatusCode.Created);

        Assert.Single(
            responses,
            x => x.StatusCode == HttpStatusCode.Conflict);

        // 9. 최종 DB 상태 확인
        using (var scope = factory.Services.CreateScope()) {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SmartWmsDbContext>();

            var product = await dbContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            Assert.Equal(
                3,
                product.StockQuantity);

            var outboundCount = await dbContext.Outbounds
                .AsNoTracking()
                .CountAsync(x =>
                    x.ProductId == productId);

            Assert.Equal(
                1,
                outboundCount);
        }
    }
}