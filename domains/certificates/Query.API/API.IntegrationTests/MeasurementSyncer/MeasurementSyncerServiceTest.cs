using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.MeasurementSyncer;

[Collection(IntegrationTestCollection.CollectionName)]
public class MeasurementSyncerServiceTest
{
    private readonly IntegrationTestFixture integrationTestFixture;
    private  DbContextOptions<ApplicationDbContext> options;

    public MeasurementSyncerServiceTest(IntegrationTestFixture integrationTestFixture)
    {
        this.integrationTestFixture = integrationTestFixture;

    }

    [Fact]
    public async Task Test1()
    {
        var emptyDb = await integrationTestFixture.PostgresContainer.CreateNewDatabase();
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(emptyDb.ConnectionString).Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var gsrn = GsrnHelper.GenerateRandom();
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);
        using var scope = integrationTestFixture.WebApplicationFactory.Services.CreateScope();
        // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Add(MeteringPointTimeSeriesSlidingWindow.Create(gsrn, UnixTimestamp.Create(now)));
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(measurement);

        await Task.Delay(TimeSpan.FromSeconds(15));

        await dbContext.SaveChangesAsync();

        var slidingWindow = await dbContext.MeteringPointTimeSeriesSlidingWindows.SingleAsync();
    }
}
