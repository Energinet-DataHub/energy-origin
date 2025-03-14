using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Repository;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementRepositoryTest
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public TransferAgreementRepositoryTest(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresDatabase.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
        new DbMigrator(newDatabaseInfo.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync()
            .Wait();
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
            StartDate = UnixTimestamp.Now(),
            EndDate = UnixTimestamp.Now().AddDays(10),
            SenderId = OrganizationId.Create(senderId),
            SenderName = OrganizationName.Create("nrgi A/S"),
            SenderTin = Tin.Create("44332211"),
            ReceiverTin = Tin.Create("12345678"),
            ReceiverReference = Guid.NewGuid(),
            TransferAgreementNumber = agreements[0].TransferAgreementNumber,
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
                StartDate = UnixTimestamp.Now(),
                EndDate = UnixTimestamp.Now().AddDays(10),
                SenderId = OrganizationId.Create(senderId),
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverTin = Tin.Create("12345678"),
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = UnixTimestamp.Now(),
                EndDate = UnixTimestamp.Now().AddDays(10),
                SenderId = OrganizationId.Create(senderId),
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverTin = Tin.Create("12345678"),
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = UnixTimestamp.Now(),
                EndDate = UnixTimestamp.Now().AddDays(10),
                SenderId = OrganizationId.Create(senderId),
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverTin = Tin.Create("12345678"),
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 3
            }
        };
        return agreements;
    }
}
