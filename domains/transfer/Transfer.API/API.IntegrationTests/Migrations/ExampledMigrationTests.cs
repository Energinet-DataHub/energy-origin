using System;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.IntegrationTests.Testcontainers;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace API.IntegrationTests.Migrations;

public class ExampledMigrationTests : IAsyncDisposable
{
    private PostgresContainer container;
    public ExampledMigrationTests()
    {
        container = new PostgresContainer();
    }

    [Fact(Skip = "This is an exampled migration test that other migration tests can be based on.")]
    //These tests can be commented out as they become deprecated. We keep this class as an example for how to write migration tests. 
    public async Task ApplyMigration_WhenExistingDataInDatabase_Success()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        //Here the database is migrated to the specific point we need.
        await migrator.MigrateAsync("20230829090644_AddInvitationsTable");

        //'Old' data (data that matches the specific point) is inserted 
        await InsertOldTransferAgreement(dbContext, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid(), "Producent A/S", "12345678", "11223344", Guid.NewGuid());
        await InsertOldTransferAgreement(dbContext, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid(), "Producent A/S2", "12345679", "11223345", Guid.NewGuid());

        //new migration is applied and we assert that we do not get an exception
        var applyMigration = () => migrator.Migrate("20230829124003_AddUniqueIndexAndTransferAgreementNumber");
        applyMigration.Should().NotThrow();

        //Assert that data is as expected
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

    private async Task<ApplicationDbContext> CreateNewCleanDatabase()
    {
        await container.InitializeAsync();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
