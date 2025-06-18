using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.IssuingContractCleanup;

[Collection(IntegrationTestCollection.CollectionName)]
public class IssuingContractCleanupTests
{
    private readonly PostgresContainer postgresContainer;

    public IssuingContractCleanupTests(IntegrationTestFixture integrationTestFixture)
    {
        postgresContainer = integrationTestFixture.PostgresContainer;
    }

    [Fact(Skip = "Contract clean up temporarily disabled")]
    public async Task ShouldOnlyDeleteExpiredIssuingContracts()
    {
        var dbContext = GetDbContext();

        dbContext.Contracts.RemoveRange(dbContext.Contracts);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expiredContract = CertificateIssuingContract.Create(
            1,
            new Gsrn("5734567890123456"),
            "932",
            MeteringPointType.Production,
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            Guid.NewGuid(),
            new Technology("SomeFuelCode", "SomeTechCode"),
            false);

        var nullEndDateContract = CertificateIssuingContract.Create(
            2,
            new Gsrn("5734567890123457"),
            "932",
            MeteringPointType.Production,
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddHours(-2),
            null, // No end date
            Guid.NewGuid(),
            new Technology("SomeFuelCode", "SomeTechCode"),
            false);

        var endDateContract =  CertificateIssuingContract.Create(
            3,
            new Gsrn("5734567890123458"),
            "932",
            MeteringPointType.Production,
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(1),
            Guid.NewGuid(),
            new Technology("SomeFuelCode", "SomeTechCode"),
            false);

        dbContext.Contracts.AddRange(expiredContract, nullEndDateContract, endDateContract);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var contracts = await dbContext.RepeatedlyQueryUntilCountIsMet<CertificateIssuingContract>(2);

        contracts.Count.Should().Be(2);
        contracts.Select(x => x.Id).Should().NotContain(expiredContract.Id);
        contracts.Select(x => x.Id).Should().Contain(nullEndDateContract.Id);
        contracts.Select(x => x.Id).Should().Contain(endDateContract.Id);
    }

    private ApplicationDbContext GetDbContext()
    {
        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(postgresContainer.ConnectionString).Options;
        return new ApplicationDbContext(contextOptions);
    }
}
