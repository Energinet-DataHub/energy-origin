using ClaimAutomation;
using DataContext;
using EnergyOrigin.Setup.Migrations;
using EnergyOrigin.WalletClient;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Worker.IntegrationTests.Factories;

public class ClaimAutomationWorkerFactory : WebApplicationFactory<Program>
{
    public IWalletClient ProjectOriginWalletClientMock { get; set; } = Substitute.For<IWalletClient>();
    public DatabaseInfo Database { get; set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ClaimAutomation:Enabled", "true");
        builder.UseSetting("ClaimAutomation:ScheduleInterval", "Every5Seconds");
        builder.UseSetting("Database:Host", Database.Host);
        builder.UseSetting("Database:Port", Database.Port);
        builder.UseSetting("Database:Name", Database.Name);
        builder.UseSetting("Database:User", Database.User);
        builder.UseSetting("Database:Password", Database.Password);

        builder.ConfigureTestServices(services =>
        {
            new DbMigrator(Database.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();

            services.Remove(services.First(s => s.ServiceType == typeof(IWalletClient)));
            services.AddSingleton(ProjectOriginWalletClientMock);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (string.IsNullOrWhiteSpace(Database.ConnectionString))
        {
            return host;
        }

        var factory = host.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        using var dbContext = factory.CreateDbContext();
        dbContext.Database.Migrate();

        return host;
    }

    public void Start() => Server.Should().NotBeNull();
}
