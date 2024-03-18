using DataContext;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Testing.Testcontainers;
using Xunit;
using FluentAssertions;

namespace API.IntegrationTests.Shared.Migrations;

public class ActivityLogEntityIdIsNowAStringTests : IAsyncDisposable
{
    private readonly PostgresContainer container = new();

    [Fact]
    public async Task ApplyMigration_WhenDataExistsInDatabase()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240125131645_AddActivityLog");

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
