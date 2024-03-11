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

public class ActivityLogEntryOtherOrgFieldsTests : MigrationsTestBase
{

    [Fact]
    public async Task GivenMigrationApplied_IfNewActivityLogEntryIsCreated_OtherOrganizationFieldsExist()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        var applyMigration = () => migrator.MigrateAsync();
        await applyMigration.Should().NotThrowAsync();

        await InsertNewActivityLogEntry(dbContext, Guid.NewGuid());

        var logEntriesInDb = dbContext.ActivityLogs.ToList();

        logEntriesInDb.Count.Should().Be(1);
        logEntriesInDb.First().OtherOrganizationTin.Should().Be("87654321");
        logEntriesInDb.First().OtherOrganizationName.Should().Be("SomeOtherOrg");
    }

    [Fact]
    public async Task GivenActivityLogExists_IfMigrationApplied_OldActivityLogsOtherOrganizationFieldsEqualStringEmpty()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var migrator = dbContext.GetService<IMigrator>();

        await migrator.MigrateAsync("20240216131219_ActivityLogEntityIdIsNowAString");

        await InsertOldActivityLogEntry(dbContext, Guid.NewGuid());

        var applyMigration = () => migrator.MigrateAsync();
        await applyMigration.Should().NotThrowAsync();

        var logEntriesInDb = dbContext.ActivityLogs.ToList();

        logEntriesInDb.Count.Should().Be(1);
        logEntriesInDb.First().OtherOrganizationName.Should().Be(string.Empty);
        logEntriesInDb.First().OtherOrganizationName.Should().Be(string.Empty);
    }

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
        var logEntryTable = dbContext.Model.FindEntityType(typeof(ActivityLogEntry))?.GetTableName();

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

    private static async Task InsertNewActivityLogEntry(ApplicationDbContext dbContext, Guid id)
    {
        var logEntryTable = dbContext.Model.FindEntityType(typeof(ActivityLogEntry))?.GetTableName();

        var logEntryQuery =
            $@"INSERT INTO ""{logEntryTable}"" (
            ""Id"",
            ""Timestamp"",
            ""ActorId"",
            ""ActorType"",
            ""ActorName"",
            ""OrganizationTin"",
            ""OrganizationName"",
            ""OtherOrganizationTin"",
            ""OtherOrganizationName"",
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
            @OtherOrganizationTin,
            @OtherOrganizationName,
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
            new NpgsqlParameter("OtherOrganizationTin", "87654321"),
            new NpgsqlParameter("OtherOrganizationName", "SomeOtherOrg"),
            new NpgsqlParameter("EntityType", 2),
            new NpgsqlParameter("ActionType", (object?)0),
            new NpgsqlParameter("EntityId", Guid.NewGuid())
        };

        await dbContext.Database.ExecuteSqlRawAsync(logEntryQuery, logEntryFields);
    }
}
