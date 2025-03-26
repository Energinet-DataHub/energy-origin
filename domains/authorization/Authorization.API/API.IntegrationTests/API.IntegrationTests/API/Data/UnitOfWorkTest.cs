using API.Data;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API.Data;

[Collection(IntegrationTestCollection.CollectionName)]
public class UnitOfWorkTest
{
    private DatabaseInfo _databaseInfo;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public UnitOfWorkTest(IntegrationTestFixture integrationTestFixture)
    {
        _databaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_databaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task GivenUnitOfWork_WhenCommitting_DataIsInserted()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken.None);
        await using var sut = new UnitOfWork(dbContext);

        await sut.BeginTransactionAsync(CancellationToken.None);
        await dbContext.Organizations.AddAsync(Any.Organization(), CancellationToken.None);
        await sut.CommitAsync(CancellationToken.None);

        await using var newDbContext = new ApplicationDbContext(_options);
        Assert.Single(newDbContext.Organizations.ToList());
    }

    [Fact]
    public async Task GivenUnitOfWork_WhenRollingBack_DataIsNotInserted()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken.None);
        await using var sut = new UnitOfWork(dbContext);

        await sut.BeginTransactionAsync(CancellationToken.None);
        await dbContext.Organizations.AddAsync(Any.Organization(), CancellationToken.None);
        await sut.RollbackAsync(CancellationToken.None);

        await using var newDbContext = new ApplicationDbContext(_options);
        Assert.Empty(newDbContext.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_OuterTransactionCommits()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken.None);
        await using (var sut1 = new UnitOfWork(dbContext))
        {
            await sut1.BeginTransactionAsync(CancellationToken.None);

            await using (new UnitOfWork(dbContext))
            {
                await dbContext.Organizations.AddAsync(Any.Organization(), CancellationToken.None);
            }

            await sut1.CommitAsync(CancellationToken.None);
        }

        await using var newDbContext = new ApplicationDbContext(_options);
        Assert.Single(newDbContext.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_OuterTransactionRollsBack()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken.None);
        await using (var sut1 = new UnitOfWork(dbContext))
        {
            await sut1.BeginTransactionAsync(CancellationToken.None);

            await using (var sut2 = new UnitOfWork(dbContext))
            {
                await sut2.BeginTransactionAsync(CancellationToken.None);
                await dbContext.Organizations.AddAsync(Any.Organization(), CancellationToken.None);
                await sut2.CommitAsync(CancellationToken.None);
            }

            await sut1.RollbackAsync(CancellationToken.None);
        }

        await using var newDbContext = new ApplicationDbContext(_options);
        Assert.Empty(newDbContext.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_InnerTransactionDoesNotRollBack()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken.None);
        await using (var sut1 = new UnitOfWork(dbContext))
        {
            await sut1.BeginTransactionAsync(CancellationToken.None);

            await using (var sut2 = new UnitOfWork(dbContext))
            {
                await sut2.BeginTransactionAsync(CancellationToken.None);
                await dbContext.Organizations.AddAsync(Any.Organization(), CancellationToken.None);
                await sut2.RollbackAsync(CancellationToken.None);
            }

            await sut1.CommitAsync(CancellationToken.None);
        }

        await using var newDbContext = new ApplicationDbContext(_options);
        Assert.Single(newDbContext.Organizations.ToList());
    }
}
