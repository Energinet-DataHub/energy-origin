using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.IssuingContractCleanup;

[Collection(IntegrationTestCollection.CollectionName)]
public class IssuingContractCleanupTests
{
    private readonly IntegrationTestFixture _integrationTestFixture;

    public IssuingContractCleanupTests(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
    }

    [Fact(Skip = "Contract clean up temporarily disabled")]
    public async Task ShouldOnlyDeleteExpiredIssuingContracts()
    {
        var dbContext = GetDbContext();

        dbContext.Contracts.RemoveRange(dbContext.Contracts);
        await dbContext.SaveChangesAsync();

        var expiredContract = new CertificateIssuingContract
        {
            GSRN = "5734567890123456",
            EndDate = DateTimeOffset.UtcNow.AddHours(-1),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 0,
            Created = DateTimeOffset.UtcNow.AddHours(-2),
            GridArea = "DK1",
            Id = Guid.NewGuid(),
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };
        var nullEndDateContract = new CertificateIssuingContract
        {
            GSRN = "5734567890123457",
            EndDate = null,
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 1,
            Created = DateTimeOffset.UtcNow.AddHours(-2),
            GridArea = "DK1",
            Id = Guid.NewGuid(),
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };
        var endDateContract = new CertificateIssuingContract
        {
            GSRN = "5734567890123458",
            EndDate = DateTimeOffset.UtcNow.AddHours(1),
            MeteringPointOwner = Guid.NewGuid().ToString(),
            ContractNumber = 2,
            Created = DateTimeOffset.UtcNow.AddHours(-2),
            GridArea = "DK1",
            Id = Guid.NewGuid(),
            MeteringPointType = MeteringPointType.Production,
            StartDate = DateTimeOffset.UtcNow.AddHours(-2),
            Technology = new Technology("SomeFuelCode", "SomeTechCode"),
            RecipientId = Guid.NewGuid()
        };
        dbContext.Contracts.AddRange(expiredContract, nullEndDateContract, endDateContract);
        await dbContext.SaveChangesAsync();

        var contracts = await dbContext.RepeatedlyQueryUntilCountIsMet<CertificateIssuingContract>(2);

        contracts.Count.Should().Be(2);
        contracts.Select(x => x.Id).Should().NotContain(expiredContract.Id);
        contracts.Select(x => x.Id).Should().Contain(nullEndDateContract.Id);
        contracts.Select(x => x.Id).Should().Contain(endDateContract.Id);
    }

    private ApplicationDbContext GetDbContext()
    {
        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_integrationTestFixture.WebApplicationFactory.ConnectionString).Options;
        return new ApplicationDbContext(contextOptions);
    }
}
