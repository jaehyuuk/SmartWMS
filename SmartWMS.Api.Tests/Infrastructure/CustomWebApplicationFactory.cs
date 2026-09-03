using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartWMS.Api.Data;

namespace SmartWMS.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {

        builder.ConfigureServices(services => {
            var descriptor = services
                .SingleOrDefault(x =>
                    x.ServiceType ==
                    typeof(DbContextOptions<SmartWmsDbContext>));

            if (descriptor is not null) {
                services.Remove(descriptor);
            }

            services.AddDbContext<SmartWmsDbContext>(options => {
                options.UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=SmartWMS_Test;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;");
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<SmartWmsDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}