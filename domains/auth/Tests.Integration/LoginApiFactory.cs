using System.Net.Http.Headers;
using API.Models;
using API.Repositories.Data;
using API.Utilities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

public class LoginApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IServiceProvider ServiceProvider => Services.CreateScope().ServiceProvider;
    public DataContext DataContext => ServiceProvider.GetRequiredService<DataContext>();

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
        builder.UseEnvironment("Test");
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

    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(User user)
    {
        var client = CreateClient();
        var userDescriptMapper = ServiceProvider.GetRequiredService<UserDescriptMapper>();
        var userDescriptor = userDescriptMapper.Map(user, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ServiceProvider.GetRequiredService<ITokenIssuer>().IssueAsync(userDescriptor));
        return client;
    }

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
        var dbContext = ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await testContainer.DisposeAsync();
    }
}
