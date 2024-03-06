using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests.IssuingContractCleanup;

public class IssuingContractCleanupTests : IClassFixture<QueryApiWebApplicationFactory>, IClassFixture<PostgresContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public IssuingContractCleanupTests(QueryApiWebApplicationFactory factory,
        PostgresContainer dbContainer)
    {
        this.factory = factory;
        this.factory.ConnectionString = dbContainer.ConnectionString;
    }

    [Fact]
    public async Task ShouldOnlyDeleteExpiredIssuingContracts()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
            WalletUrl = "http://foo",
            WalletPublicKey = { }
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
            WalletUrl = "http://foo",
            WalletPublicKey = { }
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
            WalletUrl = "http://foo",
            WalletPublicKey = { }
        };
        dbContext.Contracts.AddRange(expiredContract, nullEndDateContract, endDateContract);
        await dbContext.SaveChangesAsync();

        var contracts = await dbContext.RepeatedlyQueryUntilCountIsMet<CertificateIssuingContract>(2);

        contracts.Count.Should().Be(2);
        contracts.Select(x => x.Id).Should().NotContain(expiredContract.Id);
        contracts.Select(x => x.Id).Should().Contain(nullEndDateContract.Id);
        contracts.Select(x => x.Id).Should().Contain(endDateContract.Id);
    }
}
