using EnergyOrigin.ActivityLog.HostedService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class CleanupActivityLogsHostedServiceTests
{

    [Fact]
    public async Task ExecuteAsync_TriggersCleanupAndLogsInformation()
    {
        var logger = Substitute.For<ILogger<CleanupActivityLogsHostedService>>();
        var services = Substitute.For<IServiceProvider>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var dbContext = Substitute.For<DbContext>();
        var activityLogOptions = Substitute.For<IOptions<ActivityLogOptions>>();
        var cancellationTokenSource = new CancellationTokenSource();

        activityLogOptions.Value.Returns(new ActivityLogOptions
        {
            CleanupActivityLogsOlderThanInDays = 30,
            CleanupIntervalInSeconds = 1
        });

        services.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.GetService(typeof(DbContext)).Returns(dbContext);

        var service = new CleanupActivityLogsHostedService(logger, services, activityLogOptions);

        cancellationTokenSource.CancelAfter(1500);
        await service.StartAsync(cancellationTokenSource.Token);

        logger.ReceivedWithAnyArgs().LogInformation(default);
    }
}

