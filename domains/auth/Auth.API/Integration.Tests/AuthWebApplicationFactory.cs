using System.Net.Http.Headers;
using API.Models.Entities;
using API.Repositories.Data;
using API.Repositories.Data.Interfaces;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Integration.Tests;

public class AuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer testContainer = new PostgreSqlBuilder().Build();

    public IServiceProvider ServiceProvider => Services.CreateScope().ServiceProvider;
    public DataContext DataContext => ServiceProvider.GetRequiredService<DataContext>();

    async Task IAsyncLifetime.DisposeAsync() => await testContainer.DisposeAsync();

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();
        var dbContext = DataContext;
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.First(x => x.ServiceType == typeof(DbContextOptions<DataContext>)));
            services.Remove(services.First(x => x.ServiceType == typeof(DataContext)));
            services.Remove(services.First(x => x.ServiceType == typeof(NpgsqlDataSourceBuilder)));
            services.AddSingleton(new NpgsqlDataSourceBuilder(testContainer.GetConnectionString()));
            services.AddDbContext<DataContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSourceBuilder>().Build()));
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

    public HttpClient CreateAuthenticatedClient(User user, ProviderType providerType = ProviderType.NemIdProfessional, string? accessToken = null, string? identityToken = null, bool versionBypass = false, DateTime? issueAt = null, Action<IWebHostBuilder>? config = null)
    {
        var client = CreateAnonymousClient(config);
        var mapper = ServiceProvider.GetRequiredService<IUserDescriptorMapper>();
        var descriptor = mapper.Map(user, providerType, accessToken ?? Guid.NewGuid().ToString(), identityToken ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceProvider.GetRequiredService<ITokenIssuer>().Issue(descriptor, versionBypass, issueAt));
        return client;
    }

    public async Task<User> AddUserToDatabaseAsync(User? user = null)
    {
        user ??= new User
        {
            Name = Guid.NewGuid().ToString(),
//            AcceptedPrivacyPolicyVersion = "1",
            AllowCprLookup = true
        };

        var dbContext = DataContext;
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
