using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using MassTransit;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using RegistryConnector.Worker.IntegrationTests.Factories;
using Testing.Helpers;
using Testing.Testcontainers;
using Xunit;

namespace RegistryConnector.Worker.IntegrationTests.EventHandlers;

public class MeasurementEventHandlerTests :
    IClassFixture<PostgresContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<RegistryConnectorApplicationFactory>
{
    private readonly RegistryConnectorApplicationFactory factory;
    private readonly DbContextOptions<ApplicationDbContext> options;

    public MeasurementEventHandlerTests(
        PostgresContainer dbContainer,
        RabbitMqContainer rabbitMqContainer,
        RegistryConnectorApplicationFactory factory)
    {
        this.factory = factory;
        factory.RabbitMqOptions = rabbitMqContainer.Options;
        factory.ConnectionString = dbContainer.ConnectionString;
        factory.Start();

        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    [Theory]
    [InlineData(MeasurementQuality.Calculated)]
    [InlineData(MeasurementQuality.Measured)]
    public async Task ShouldOnlyProcessMessagesOnSaveChanges(MeasurementQuality quality)
    {
        var privateKey = new Secp256k1Algorithm().GenerateNewPrivateKey();
        var hdPublicKey = privateKey.Derive(1).Neuter();
        var gsrn = GsrnHelper.GenerateRandom();
        var subject = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: quality);

        using (var scope = factory.ServiceScope())
        {
            using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.Contracts.Add(new CertificateIssuingContract
                {
                    MeteringPointType = MeteringPointType.Consumption,
                    ContractNumber = 1,
                    Created = DateTimeOffset.UtcNow,
                    EndDate = null,
                    StartDate = utcMidnight,
                    GSRN = gsrn,
                    GridArea = "DK1",
                    Id = Guid.NewGuid(),
                    MeteringPointOwner = subject,
                    Technology = null,
                    WalletUrl = "https://foo",
                    WalletPublicKey = hdPublicKey.Export().ToArray(),
                });

                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(measurement);

                //We wait and confirm that the publish does not happen before SaveChanges has been called
                //(i.e. before the message has been put in the database)
                await Task.Delay(TimeSpan.FromSeconds(15));

                await dbContext.SaveChangesAsync();
            }
        }

        (await IsConsumptionCertificateSaved(gsrn)).Should().Be(true);
    }

    private async Task<bool> IsConsumptionCertificateSaved(string gsrn)
    {
        var limit = TimeSpan.FromSeconds(30);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            using (var dbContext = new ApplicationDbContext(options))
            {
                var consumptionCert = dbContext.ConsumptionCertificates.FirstOrDefault(x => x.Gsrn == gsrn);

                if (consumptionCert != null)
                    return true;

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        } while (stopwatch.Elapsed < limit);

        return false;
    }
}
