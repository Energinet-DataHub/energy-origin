using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.ActivityLog.HostedService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class CleanupActivityLogsHostedServiceTests
{
    [Fact]
    public async Task CleanupService_DeletesOldActivityLogs_LeavesRecentLogs()
    {
        // Setup in-memory database context
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;


        var dbContext = new TestDbContext(options);

        // Seed database with test data
        await SeedDatabaseAsync(dbContext);

        // Setup service dependencies
        var logger = NSubstitute.Substitute.For<ILogger<CleanupActivityLogsHostedService>>();
        var serviceProvider = new ServiceCollection()
            .AddScoped<DbContext>(provider => dbContext)
            .BuildServiceProvider();

        var activityLogOptions = Options.Create(new ActivityLogOptions
        {
            CleanupIntervalInSeconds = 1,
            CleanupActivityLogsOlderThanInDays = 1
        });

        var service = new CleanupActivityLogsHostedService(logger, serviceProvider, activityLogOptions);

        // Mimic background service execution
        var stoppingToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
        await service.StartAsync(stoppingToken);

        // Assertion: Only the future-dated log should remain
        Assert.Equal(1, dbContext.ActivityLogEntries.Count());
        Assert.True(dbContext.ActivityLogEntries.Any(log => log.Timestamp > DateTimeOffset.UtcNow));
    }

    private static async Task SeedDatabaseAsync(DbContext context)
    {
        await context.Set<ActivityLogEntry>().AddRangeAsync([
            ActivityLogEntry.Create(
                Guid.NewGuid(),
                ActivityLogEntry.ActorTypeEnum.User,
                "Test User 1",
                "12345678",
                "Test Organization 1",
                "87654321",
                "Other Test Organization 1",
                ActivityLogEntry.EntityTypeEnum.TransferAgreement,
                ActivityLogEntry.ActionTypeEnum.Created,
                "TestEntityId1"
            ),
            ActivityLogEntry.Create(
                Guid.NewGuid(),
                ActivityLogEntry.ActorTypeEnum.System,
                "Test User 2",
                "87654321",
                "Test Organization 2",
                "98765432",
                "Other Test Organization 2",
                ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                ActivityLogEntry.ActionTypeEnum.Accepted,
                "TestEntityId2")
        ]);
        await context.SaveChangesAsync();
    }
}
