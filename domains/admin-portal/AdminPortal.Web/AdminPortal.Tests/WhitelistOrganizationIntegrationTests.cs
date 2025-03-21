using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Models;
using AdminPortal.Tests.Setup;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminPortal.Tests;

public class WhitelistOrganizationIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainer _postgresContainer = new();
    private TestWebApplicationFactory? _factory;
    private HttpClient? _client;
    private string? _connectionString;

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.InitializeAsync();

        // Create isolated database
        var databaseInfo = await _postgresContainer.CreateNewDatabase();
        _connectionString = databaseInfo.ConnectionString;

        // Initialize factory with valid configuration
        _factory = new TestWebApplicationFactory();
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(_connectionString));
            });
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _client = _factory.CreateAuthenticatedClient<GeneralUser>(
            new WebApplicationFactoryClientOptions(),
            sessionId: 12345
        ) ?? throw new NullReferenceException("Failed to create HTTP client");
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
            _client.Dispose();

        if (_factory != null)
            await _factory.DisposeAsync();

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task POST_ValidTin_AddsToOutbox()
    {
        if (_client == null)
            throw new NullReferenceException("HTTP client not initialized");

        var validTin = "12345678";

        var response = await _client.PostAsync("/ett-admin-portal/WhitelistedOrganizations/WhitelistFirstPartyOrganization", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Tin", validTin) })
, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using (var scope = _factory!.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var outboxCount = await dbContext.Database
                .SqlQuery<int>($"SELECT COUNT(*) FROM public.\"OutboxMessage\"")
                .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(outboxCount > 0);
        }
    }
}
