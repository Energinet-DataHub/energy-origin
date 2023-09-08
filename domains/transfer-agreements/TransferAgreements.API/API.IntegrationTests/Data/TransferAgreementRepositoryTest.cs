using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.IntegrationTests.Testcontainers;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Data;

public class TransferAgreementRepositoryTest : IClassFixture<PostgresContainer>
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public TransferAgreementRepositoryTest(PostgresContainer dbContainer)
    {
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbContainer.ConnectionString).Options;
    }

    [Fact]
    public async Task AddTransferAgreementToDb_SameSenderIdAndTransferAgreementNumber_ShouldThrowException()
    {
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var repository = new TransferAgreementRepository(dbContext);

        var senderId = Guid.NewGuid();
        var agreements = SetupTransferAgreements(senderId);

        foreach (var agreement in agreements)
        {
            await repository.AddTransferAgreementToDb(agreement);
        }

        var existingTransferAgreement = dbContext.TransferAgreements.First();

        var transferAgreement = new TransferAgreement()
        {
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            SenderId = senderId,
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverTin = "12345678",
            ReceiverReference = Guid.NewGuid(),
            TransferAgreementNumber = existingTransferAgreement.TransferAgreementNumber
        };
        dbContext.TransferAgreements.Add(transferAgreement);
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private static List<TransferAgreement> SetupTransferAgreements(Guid senderId)
    {
        var agreements = new List<TransferAgreement>()
        {
            new()
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
            },
            new()
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
            },
            new()
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid(),
            }
        };
        return agreements;
    }
}
