using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Repository;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementRepositoryTest
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public TransferAgreementRepositoryTest(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task AddTransferAgreementToDb_SameSenderIdAndTransferAgreementNumber_ShouldThrowException()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var senderId = Guid.NewGuid();
        var agreements = SetupTransferAgreements(senderId);
        await TestData.SeedTransferAgreements(dbContext, agreements);

        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            SenderId = senderId,
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverTin = "12345678",
            ReceiverReference = Guid.NewGuid(),
            TransferAgreementNumber = agreements[0].TransferAgreementNumber
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => TestData.SeedTransferAgreementsSaveChangesAsync(dbContext, transferAgreement));
    }

    private static List<TransferAgreement> SetupTransferAgreements(Guid senderId)
    {
        var agreements = new List<TransferAgreement>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 3

            }
        };
        return agreements;
    }
}
