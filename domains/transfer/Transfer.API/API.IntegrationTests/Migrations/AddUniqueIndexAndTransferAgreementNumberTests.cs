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

    [Fact(Skip = "This is an exampled migration test that other migration tests can be based on.")]
    public async Task ApplyMigration_WhenExistingDataInDatabase_Success()
    {
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync("20230829090644_AddInvitationsTable");
        await dbContext.TruncateTransferAgreementsTable();

        await InsertOldTransferAgreement(dbContext, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid(), "Producent A/S", "12345678", "11223344", Guid.NewGuid());
        await InsertOldTransferAgreement(dbContext, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid(), "Producent A/S2", "12345679", "11223345", Guid.NewGuid());

        var applyMigration = () => migrator.Migrate("20230829124003_AddUniqueIndexAndTransferAgreementNumber");
        applyMigration.Should().NotThrow();

        //This last part can be deleted should the TransferAgreements table change in the future
        var tas = dbContext.TransferAgreements.ToList();

        tas.Count.Should().Be(2);
    }

    private static async Task InsertOldTransferAgreement(ApplicationDbContext dbContext, Guid id, DateTimeOffset startDate, DateTimeOffset endDate, Guid senderId, string senderName,
        string senderTin, string receiverTin, Guid receiverReference)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement))!.GetTableName();

        var agreementQuery =
            $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverReference\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverReference)";
        var agreementFields = new[]
        {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("StartDate", startDate),
            new NpgsqlParameter("EndDate", endDate),
            new NpgsqlParameter("SenderId", senderId),
            new NpgsqlParameter("SenderName", senderName),
            new NpgsqlParameter("SenderTin", senderTin),
            new NpgsqlParameter("ReceiverTin", receiverTin),
            new NpgsqlParameter("ReceiverReference", receiverReference)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }
}
