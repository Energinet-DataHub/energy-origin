using API;
using API.Repositories.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class LoginApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlTestcontainer testContainer
        = new ContainerBuilder<PostgreSqlTestcontainer>()
       .WithDatabase(new PostgreSqlTestcontainerConfiguration
       {
           Database = "Database",
           Username = "admin",
           Password = "admin",
       })
       .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.First(s => s.ServiceType == typeof(DbContextOptions<DataContext>)));
            services.Remove(services.First(s => s.ServiceType == typeof(DataContext)));
            services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(testContainer.ConnectionString);
            });
            services.AddScoped<IUserDataContext, DataContext>();
        });
    }
    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
        var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await testContainer.DisposeAsync();
    }
}
