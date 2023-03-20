using System.Net.Http.Headers;
using API.Models.Entities;
using API.Repositories.Data;
using API.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Tests.Integration;

public class AuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().Build();

    public IServiceProvider ServiceProvider => Services.CreateScope().ServiceProvider;
    public DataContext DataContext => ServiceProvider.GetRequiredService<DataContext>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.First(x => x.ServiceType == typeof(DbContextOptions<DataContext>)));
            services.Remove(services.First(x => x.ServiceType == typeof(DataContext)));
            services.AddDbContext<DataContext>(options => options.UseNpgsql(testContainer.GetConnectionString()));
            services.AddScoped<IUserDataContext, DataContext>();
        });
    }

    public HttpClient CreateAnonymousClient(Action<IWebHostBuilder>? config = null)
    {
        var factory = config is not null ? WithWebHostBuilder(config) : this;
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public HttpClient CreateAuthenticatedClient(User user, string? accessToken = null, string? identityToken = null, bool versionBypass = false, DateTime? issueAt = null, Action<IWebHostBuilder>? config = null)
    {
        var client = CreateAnonymousClient(config);
        var claimsWrapperMapper = ServiceProvider.GetRequiredService<IClaimsWrapperMapper>();
        var claimsWrapper = claimsWrapperMapper.Map(user, accessToken ?? Guid.NewGuid().ToString(), identityToken ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceProvider.GetRequiredService<ITokenIssuer>().Issue(claimsWrapper, versionBypass, issueAt));
        return client;
    }

    public async Task<User> AddUserToDatabaseAsync(User? user = null)
    {
        user ??= new User()
        {
            ProviderId = Guid.NewGuid().ToString(),
            Name = Guid.NewGuid().ToString(),
            AcceptedTermsVersion = 1,
            AllowCPRLookup = true
        };

        var dbContext = DataContext;
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
        var dbContext = DataContext;
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await testContainer.DisposeAsync();
}
