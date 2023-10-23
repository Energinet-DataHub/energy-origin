# Migration testing

To prevent errors in production it is expected to write migration integration tests when making migrations. These tests can be commented out when they break.

Below is an exampled migration test. For the full example see Transfer domain -> API.IntegrationsTests/Migrations/ExampledMigrationTests.cs.

```
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

```
