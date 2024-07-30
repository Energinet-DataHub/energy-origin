using NSubstitute;
using Microsoft.EntityFrameworkCore;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class ActivityLogEntryRepositoryTests
{
    private readonly DbContext _dbContext;
    private readonly IActivityLogEntryRepository _repository;

    public ActivityLogEntryRepositoryTests()
    {
        _dbContext = Substitute.For<DbContext>();
        _repository = new ActivityLogEntryRepository(_dbContext);

        var dbSet = Substitute.For<DbSet<ActivityLogEntry>, IEnumerable<ActivityLogEntry>>();
        using var enumerator = ((IEnumerable<ActivityLogEntry>)dbSet).GetEnumerator();

        _dbContext.Set<ActivityLogEntry>().Returns(dbSet);
    }

    [Fact]
    public async Task AddActivityLogEntryAsync_ShouldAddEntryToDbContext()
    {
        var actorId = Guid.NewGuid();
        var actorType = ActivityLogEntry.ActorTypeEnum.User;
        var actorName = "Test User";
        var organizationTin = "12345678";
        var organizationName = "Test Organization";
        var otherOrganizationTin = "87654321";
        var otherOrganizationName = "Other Test Organization";
        var entityType = ActivityLogEntry.EntityTypeEnum.TransferAgreement;
        var actionType = ActivityLogEntry.ActionTypeEnum.Created;
        var entityId = "TestEntityId";

        var entry = ActivityLogEntry.Create(
            actorId,
            actorType,
            actorName,
            organizationTin,
            organizationName,
            otherOrganizationTin,
            otherOrganizationName,
            entityType,
            actionType,
            entityId
        );

        await _repository.AddActivityLogEntryAsync(entry);

        await _dbContext.Received().Set<ActivityLogEntry>().AddAsync(entry);
    }

    [Fact]
    public void Create_ShouldReturnCorrectActivityLogEntry()
    {
        var actorId = Guid.NewGuid();
        var actorType = ActivityLogEntry.ActorTypeEnum.User;
        var actorName = "Test User";
        var organizationTin = "12345678";
        var organizationName = "Test Organization";
        var otherOrganizationTin = "87654321";
        var otherOrganizationName = "Other Test Organization";
        var entityType = ActivityLogEntry.EntityTypeEnum.TransferAgreement;
        var actionType = ActivityLogEntry.ActionTypeEnum.Created;
        var entityId = "TestEntityId";

        var result = ActivityLogEntry.Create(
            actorId,
            actorType,
            actorName,
            organizationTin,
            organizationName,
            otherOrganizationTin,
            otherOrganizationName,
            entityType,
            actionType,
            entityId
        );

        Assert.Equal(actorId, result.ActorId);
        Assert.Equal(actorType, result.ActorType);
        Assert.Equal(actorName, result.ActorName);
        Assert.Equal(organizationTin, result.OrganizationTin);
        Assert.Equal(organizationName, result.OrganizationName);
        Assert.Equal(otherOrganizationTin, result.OtherOrganizationTin);
        Assert.Equal(otherOrganizationName, result.OtherOrganizationName);
        Assert.Equal(entityType, result.EntityType);
        Assert.Equal(actionType, result.ActionType);
        Assert.Equal(entityId, result.EntityId);
    }

    [Fact]
    public async Task GetActivityLogAsync_FiltersEntriesCorrectly()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_GetActivityLogAsync") // Ensure unique name for parallel test execution
            .Options;

        await using (var context = new TestDbContext(options))
        {
            await context.ActivityLogEntries.AddRangeAsync(new List<ActivityLogEntry>
            {
                ActivityLogEntry.Create(
                    Guid.NewGuid(),
                    ActivityLogEntry.ActorTypeEnum.User,
                    "Test User 1",
                    "12345678",
                    "Test Organization 1",
                    "87654321",
                    "Other Test Organization 1",
                    ActivityLogEntry.EntityTypeEnum.TransferAgreement,
                    ActivityLogEntry.ActionTypeEnum.Created,
                    "TestEntityId1"
                ),
                ActivityLogEntry.Create(
                    Guid.NewGuid(),
                    ActivityLogEntry.ActorTypeEnum.System,
                    "Test User 1",
                    "12345678",
                    "Test Organization 1",
                    "98765432",
                    "Other Test Organization 2",
                    ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                    ActivityLogEntry.ActionTypeEnum.Accepted,
                    "TestEntityId2"
                )
            });
            await context.SaveChangesAsync();
        }

        await using (var context = new TestDbContext(options))
        {
            var repository = new ActivityLogEntryRepository(context);
            var startUnix = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
            var endUnix = DateTimeOffset.UtcNow.AddDays(+7).ToUnixTimeSeconds();
            var request = new ActivityLogEntryFilterRequest(startUnix, endUnix, ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement);

            var result = await repository.GetActivityLogAsync("12345678", request);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("TestEntityId1", result.First().EntityId);
        }
    }
}
