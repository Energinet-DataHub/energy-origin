using System.Net.Http.Headers;
using API.Models.Entities;
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

public class AuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IServiceProvider ServiceProvider => Services.CreateScope().ServiceProvider;

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
            services.Remove(services.First(x => x.ServiceType == typeof(DbContextOptions<DataContext>)));
            services.Remove(services.First(x => x.ServiceType == typeof(DataContext)));
            services.AddDbContext<DataContext>(options => options.UseNpgsql(testContainer.ConnectionString));
            services.AddScoped<IUserDataContext, DataContext>();
        });
    }

    public HttpClient CreateUnauthenticatedClient(Action<IWebHostBuilder>? config = null)
    {
        var factory = config is not null ? WithWebHostBuilder(config) : this;
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(User user, string? accessToken = null, string? identityToken = null, Action<IWebHostBuilder>? config = null)
    {
        var client = CreateUnauthenticatedClient(config);
        var userDescriptMapper = ServiceProvider.GetRequiredService<IUserDescriptMapper>();
        var userDescriptor = userDescriptMapper.Map(user, accessToken ?? Guid.NewGuid().ToString(), identityToken ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ServiceProvider.GetRequiredService<ITokenIssuer>().IssueAsync(userDescriptor));
        return client;
    }

    public async Task<User> AddUserToDatabaseAsync(User? user = null)
    {
        user ??= new User()
        {
            ProviderId = Guid.NewGuid().ToString(),
            Name = Guid.NewGuid().ToString(),
            AcceptedTermsVersion = 1,
            Tin = null,
            AllowCPRLookup = true
        };

        var dbContext = ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
        var dbContext = ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await testContainer.DisposeAsync();
}
