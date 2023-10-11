using System.Net.Http.Headers;
using API.Models.Entities;
using API.Repositories.Data;
using API.Repositories.Data.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Testcontainers.PostgreSql;
using static API.Utilities.TokenIssuer;

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
            services.AddDbContextFactory<DataContext>();
            services.Remove(services.First(x => x.ServiceType == typeof(DbContextOptions<DataContext>)));
            services.Remove(services.First(x => x.ServiceType == typeof(DataContext)));
            services.Remove(services.First(x => x.ServiceType == typeof(NpgsqlDataSourceBuilder)));
            services.AddSingleton(new NpgsqlDataSourceBuilder(testContainer.GetConnectionString()));
            services.AddDbContext<DataContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSourceBuilder>().Build()));
            services.AddScoped<IUserDataContext, DataContext>();
            services.Configure<HealthCheckServiceOptions>(x =>
            {
                var registration = x.Registrations.FirstOrDefault(x => x.Name.ToLower().Equals("npgsql"));
                if (registration != null)
                {
                    x.Registrations.Remove(registration);
                }
            });
            services.AddHealthChecks().AddNpgSql(testContainer.GetConnectionString());
        });
    }

    public bool Start() => Server != null;

    public HttpClient CreateAnonymousClient(Action<IWebHostBuilder>? config = null)
    {
        var factory = config is not null ? WithWebHostBuilder(config) : this;
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public HttpClient CreateAuthenticatedClient(User user, ProviderType providerType = ProviderType.NemIdProfessional, string? accessToken = null, string? identityToken = null, string? role = null, IEnumerable<string>? roles = null, bool versionBypass = false, DateTime? issueAt = null, Action<IWebHostBuilder>? config = null)
    {
        var cryptography = new Cryptography(new CryptographyOptions
        {
            Key = "secretsecretsecretsecret"
        });
        var matchedRoles = new[] { role }.OfType<string>().Concat(roles ?? Array.Empty<string>());
        var client = CreateAnonymousClient(config);
        var descriptor = user.MapDescriptor(cryptography, providerType, matchedRoles, accessToken ?? Guid.NewGuid().ToString(), identityToken ?? Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceProvider.GetRequiredService<ITokenIssuer>().Issue(descriptor, UserData.From(user), versionBypass, issueAt));
        return client;
    }

    public async Task<User> AddUserToDatabaseAsync(User? user = null)
    {
        user ??= new User
        {
            Name = Guid.NewGuid().ToString(),
            AllowCprLookup = true
        };

        var dbContext = DataContext;
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
