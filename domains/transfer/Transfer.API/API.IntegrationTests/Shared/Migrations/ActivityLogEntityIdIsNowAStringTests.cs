using System;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using EnergyOrigin.ActivityLog.DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace API.IntegrationTests.Shared.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class ActivityLogEntityIdIsNowAStringTests(IntegrationTestFixture integrationTestFixture) : MigrationsTestBase(integrationTestFixture)
{
    [Fact]
    public async Task ApplyMigration_WhenDataExistsInDatabase()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240125123642_AddActivitylogEntity");

        await InsertOldActivityLogEntry(dbContext, Guid.NewGuid());

        var applyMigration = () => migrator.MigrateAsync();
        await applyMigration.Should().NotThrowAsync();

        var logEntriesInDb = dbContext.ActivityLogs.ToList();

        logEntriesInDb.Count.Should().Be(1);
    }

    private static async Task InsertOldActivityLogEntry(ApplicationDbContext dbContext, Guid id)
    {
        var logEntryTable = dbContext.Model.FindEntityType(typeof(ActivityLogEntry))!.GetTableName();

        var logEntryQuery =
            $@"INSERT INTO ""{logEntryTable}"" (
            ""Id"",
            ""Timestamp"",
            ""ActorId"",
            ""ActorType"",
            ""ActorName"",
            ""OrganizationTin"",
            ""OrganizationName"",
            ""EntityType"",
            ""ActionType"",
            ""EntityId"")
        VALUES (
            @Id,
            @Timestamp,
            @ActorId,
            @ActorType,
            @ActorName,
            @OrganizationTin,
            @OrganizationName,
            @EntityType,
            @ActionType,
            @EntityId)";

        object[] logEntryFields = {
            new NpgsqlParameter("Id", id),
            new NpgsqlParameter("Timestamp", DateTimeOffset.UtcNow),
            new NpgsqlParameter("ActorId", Guid.NewGuid()),
            new NpgsqlParameter("ActorType", (object?)0),
            new NpgsqlParameter("ActorName", "SomeActor"),
            new NpgsqlParameter("OrganizationTin", "12345678"),
            new NpgsqlParameter("OrganizationName", "SomeOrg"),
            new NpgsqlParameter("EntityType", 2),
            new NpgsqlParameter("ActionType", (object?)0),
            new NpgsqlParameter("EntityId", Guid.NewGuid())
        };

        await dbContext.Database.ExecuteSqlRawAsync(logEntryQuery, logEntryFields);
    }
}
