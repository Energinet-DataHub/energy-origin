using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.ActivityLog.HostedService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class CleanupActivityLogsHostedServiceTests
{
    private readonly ILogger<CleanupActivityLogsHostedService> _logger = Substitute.For<ILogger<CleanupActivityLogsHostedService>>();
    private readonly IServiceProvider _services = Substitute.For<IServiceProvider>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly DbContext _dbContext = Substitute.For<DbContext>();
    private readonly IOptions<ActivityLogOptions> _activityLogOptions = Substitute.For<IOptions<ActivityLogOptions>>();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [Fact]
    public async Task ExecuteAsync_TriggersCleanupAndLogsInformation()
    {
        _activityLogOptions.Value.Returns(new ActivityLogOptions
        {
            CleanupActivityLogsOlderThanInDays = 30,
            CleanupIntervalInSeconds = 1
        });

        _services.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.GetService(typeof(DbContext)).Returns(_dbContext);

        var service = new CleanupActivityLogsHostedService(_logger, _services, _activityLogOptions);

        _cancellationTokenSource.CancelAfter(1500);
        await service.StartAsync(_cancellationTokenSource.Token);

        _logger.ReceivedWithAnyArgs().LogInformation(default);
    }

    [Fact]
    public async Task GivenActivityLogEntries_WhenCleaningUpOldEntries_EntriesAreRemoved()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetActivityLogAsync") // Ensure unique name for parallel test execution
            .Options;
        await using var dbContext = new TestDbContext(options);

        await dbContext.ActivityLogEntries.AddAsync(ActivityLogEntry.Create(Guid.NewGuid(), ActivityLogEntry.ActorTypeEnum.User, "a", "12345678", "b",
            "87654321", "c", ActivityLogEntry.EntityTypeEnum.TransferAgreement, ActivityLogEntry.ActionTypeEnum.Deactivated, "123"));

        _activityLogOptions.Value.Returns(new ActivityLogOptions
        {
            CleanupActivityLogsOlderThanInDays = -1,
            CleanupIntervalInSeconds = 1
        });

        _services.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.GetService(typeof(DbContext)).Returns(dbContext);

        var service = new CleanupActivityLogsHostedService(_logger, _services, _activityLogOptions);

        var timeout = 1500;
        _cancellationTokenSource.CancelAfter(timeout);
        await service.StartAsync(_cancellationTokenSource.Token);
        await WaitForCancellation(_cancellationTokenSource, timeout);

        await using var newDbContext = new TestDbContext(options);
        Assert.Empty(newDbContext.ActivityLogEntries);
    }

    private async Task WaitForCancellation(CancellationTokenSource tokenSource, int timeout)
    {
        try
        {
            await Task.Delay(timeout, tokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
    }
}
