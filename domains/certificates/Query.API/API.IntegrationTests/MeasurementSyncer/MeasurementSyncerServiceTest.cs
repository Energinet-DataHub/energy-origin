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
    private readonly DbContextOptions<ApplicationDbContext> options;

    public MeasurementSyncerServiceTest(IntegrationTestFixture integrationTestFixture)
    {
        this.integrationTestFixture = integrationTestFixture;
        var emptyDb = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(emptyDb.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Test1()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await using (var dbContext = new ApplicationDbContext(options))
        {
            dbContext.Add(MeteringPointTimeSeriesSlidingWindow.Create(gsrn, UnixTimestamp.Create(now)));
            using var scope = integrationTestFixture.WebApplicationFactory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(measurement);

            await Task.Delay(TimeSpan.FromSeconds(15));

            await dbContext.SaveChangesAsync();
        }

    }
}
