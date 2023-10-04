using System;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.IntegrationTests.Factories;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace API.IntegrationTests.Migrations;

public class AddUniqueIndexAndTransferAgreementNumberTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public AddUniqueIndexAndTransferAgreementNumberTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ApplyMigration_WhenExistingDataInDatabase_Success()
    {
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync("20230829090644_AddInvitationsTable");
        await dbContext.TruncateTransferAgreementsTable();

        await InsertOldTransferAgreement(dbContext, new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "11223344",
            SenderId = Guid.NewGuid(),
            SenderName = "Producent A/S",
            SenderTin = "12345678",
            StartDate = DateTimeOffset.UtcNow
        });
        await InsertOldTransferAgreement(dbContext, new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "11223345",
            SenderId = Guid.NewGuid(),
            SenderName = "Producent A/S2",
            SenderTin = "12345679",
            StartDate = DateTimeOffset.UtcNow
        });

        var applyMigration = () => migrator.Migrate("20230829124003_AddUniqueIndexAndTransferAgreementNumber");
        applyMigration.Should().NotThrow();

        var tas = dbContext.TransferAgreements.ToList();

        tas.Count.Should().Be(2);

    }

    [Fact]
    public async Task ApplyMigration_WhenTransferAgreementsHasSameSenderId_ExpectError()
    {
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync("20230829090644_AddInvitationsTable");
        await dbContext.TruncateTransferAgreementsTable();

        var senderId = Guid.NewGuid();
        await InsertOldTransferAgreement(dbContext, new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "11223344",
            SenderId = senderId,
            SenderName = "Producent A/S",
            SenderTin = "12345678",
            StartDate = DateTimeOffset.UtcNow
        });
        await InsertOldTransferAgreement(dbContext, new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "11223345",
            SenderId = senderId,
            SenderName = "Producent A/S2",
            SenderTin = "12345679",
            StartDate = DateTimeOffset.UtcNow
        });

        var applyMigration = () => migrator.Migrate("20230829124003_AddUniqueIndexAndTransferAgreementNumber");
        applyMigration.Should().Throw<PostgresException>();
    }

    private static async Task InsertOldTransferAgreement(ApplicationDbContext dbContext, TransferAgreement agreement)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement)).GetTableName();

        var agreementQuery =
            $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverReference\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverReference)";
        var agreementFields = new[]
        {
            new NpgsqlParameter("Id", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin),
            new NpgsqlParameter("ReceiverReference", agreement.ReceiverReference)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }
}
