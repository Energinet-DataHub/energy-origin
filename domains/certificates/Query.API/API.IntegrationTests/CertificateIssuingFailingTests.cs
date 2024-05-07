using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using DataContext.ValueObjects;
using FluentAssertions;
using MeasurementEvents;
using System;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using Testing.Helpers;
using Testing.Testcontainers;
using Xunit;
using Microsoft.EntityFrameworkCore;
using DataContext.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace API.IntegrationTests;

public sealed class CertificateIssuingFailingTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<MeasurementsWireMock>,
    IClassFixture<RegistryConnectorApplicationFactory>,
    IClassFixture<ProjectOriginStack>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly MeasurementsWireMock measurementsWireMock;

    public CertificateIssuingFailingTests(
        QueryApiWebApplicationFactory factory,
        PostgresContainer dbContainer,
        RabbitMqContainer rabbitMqContainer,
        MeasurementsWireMock measurementsWireMock,
        RegistryConnectorApplicationFactory registryConnectorFactory,
        ProjectOriginStack projectOriginStack)
    {
        this.measurementsWireMock = measurementsWireMock;
        this.factory = factory;
        this.factory.ConnectionString = dbContainer.ConnectionString;
        this.factory.MeasurementsUrl = measurementsWireMock.Url;
        this.factory.WalletUrl = projectOriginStack.WalletUrl;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.RetryOptions.RegistryTransactionStillProcessingRetryCount = 1;
        registryConnectorFactory.RetryOptions.DefaultFirstLevelRetryCount = 1;
        registryConnectorFactory.RetryOptions.DefaultSecondLevelRetryCount = 1;
        registryConnectorFactory.RabbitMqOptions = rabbitMqContainer.Options;
        registryConnectorFactory.PoRegistryOptions = projectOriginStack.Options;
        registryConnectorFactory.PoRegistryOptions.RegistryUrl = "https://someurl.com";
        registryConnectorFactory.ConnectionString = dbContainer.ConnectionString;
        registryConnectorFactory.Start();
    }

    [Fact]
    public async Task MeasurementFromProductionMeteringPointAddedToBus_WhenRegistryIsDown_RejectCertificate()
    {
        var subject = Guid.NewGuid().ToString();
        var gsrn = GsrnHelper.GenerateRandom();

        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay);

        await factory.AddContract(subject, gsrn, utcMidnight, MeteringPointType.Production, measurementsWireMock);

        var measurement = new EnergyMeasuredIntegrationEvent(
            GSRN: gsrn,
            DateFrom: utcMidnight.ToUnixTimeSeconds(),
            DateTo: utcMidnight.AddHours(1).ToUnixTimeSeconds(),
            Quantity: 42,
            Quality: MeasurementQuality.Measured);

        await factory.GetMassTransitBus().Publish(measurement);

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(factory.ConnectionString)
            .Options;
        await using var dbContext = new ApplicationDbContext(contextOptions);

        var cert = await RepeatedlyQueryProductionCertsUntilRejected(dbContext, 1);

        cert.First().IsRejected.Should().BeTrue();
    }

    private static async Task<List<ProductionCertificate>> RepeatedlyQueryProductionCertsUntilRejected(ApplicationDbContext dbContext, int count, TimeSpan? timeLimit = null)
    {
        var limit = timeLimit ?? TimeSpan.FromSeconds(15);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        do
        {
            var entities = await dbContext.ProductionCertificates.Where(c => c.IssuedState == IssuedState.Rejected).ToListAsync();

            if (entities.Count == count)
                return entities;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        } while (stopwatch.Elapsed < limit);

        throw new Exception($"Entity not found within the time limit ({limit.TotalSeconds} seconds)");
    }
}
