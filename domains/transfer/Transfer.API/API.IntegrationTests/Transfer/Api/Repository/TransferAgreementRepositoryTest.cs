using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Repository;

public class TransferAgreementRepositoryTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public TransferAgreementRepositoryTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AddTransferAgreementToDb_SameSenderIdAndTransferAgreementNumber_ShouldThrowException()
    {
        var senderId = Guid.NewGuid();
        var agreements = SetupTransferAgreements(senderId);
        await factory.SeedTransferAgreements(agreements);


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

        await Assert.ThrowsAsync<DbUpdateException>(() => factory.SeedTransferAgreementsSaveChangesAsync(transferAgreement));
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
