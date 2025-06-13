using API.Data;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API.Data;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class UnitOfWorkTest : IntegrationTestBase
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public UnitOfWorkTest(IntegrationTestFixture fixture) : base(fixture)
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                   .UseNpgsql(fixture.ConnectionString)
                   .Options;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        await using var db = new ApplicationDbContext(_options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task GivenUnitOfWork_WhenCommitting_DataIsInserted()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await using var uow = new UnitOfWork(ctx);

        await uow.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        await using var verify = new ApplicationDbContext(_options);
        Assert.Single(verify.Organizations.ToList());
    }

    [Fact]
    public async Task GivenUnitOfWork_WhenRollingBack_DataIsNotInserted()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await using var uow = new UnitOfWork(ctx);

        await uow.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
        await uow.RollbackAsync(TestContext.Current.CancellationToken);

        await using var verify = new ApplicationDbContext(_options);
        Assert.Empty(verify.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_OuterTransactionCommits()
    {
        await using var ctx = new ApplicationDbContext(_options);

        await using (var outer = new UnitOfWork(ctx))
        {
            await outer.BeginTransactionAsync(TestContext.Current.CancellationToken);

            // inner UoW inherits the same DbContext / transaction
            await using (new UnitOfWork(ctx))
            {
                await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
            }

            await outer.CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = new ApplicationDbContext(_options);
        Assert.Single(verify.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_OuterTransactionRollsBack()
    {
        await using var ctx = new ApplicationDbContext(_options);

        await using (var outer = new UnitOfWork(ctx))
        {
            await outer.BeginTransactionAsync(TestContext.Current.CancellationToken);

            await using (var inner = new UnitOfWork(ctx))
            {
                await inner.BeginTransactionAsync(TestContext.Current.CancellationToken);
                await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
                await inner.CommitAsync(TestContext.Current.CancellationToken);
            }

            await outer.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = new ApplicationDbContext(_options);
        Assert.Empty(verify.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenCommitting_InnerTransactionDoesNotRollBack()
    {
        await using var ctx = new ApplicationDbContext(_options);

        await using (var outer = new UnitOfWork(ctx))
        {
            await outer.BeginTransactionAsync(TestContext.Current.CancellationToken);

            await using (var inner = new UnitOfWork(ctx))
            {
                await inner.BeginTransactionAsync(TestContext.Current.CancellationToken);
                await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
                await inner.RollbackAsync(TestContext.Current.CancellationToken);
            }

            await outer.CommitAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = new ApplicationDbContext(_options);
        Assert.Single(verify.Organizations.ToList());
    }

    [Fact]
    public async Task GivenNestedUnitOfWorks_WhenThrowingException_OuterTransactionRollsBack()
    {
        var exceptionThrown = false;

        try
        {
            await using var ctx = new ApplicationDbContext(_options);

            await using (var outer = new UnitOfWork(ctx))
            {
                await outer.BeginTransactionAsync(TestContext.Current.CancellationToken);

                await using (var inner = new UnitOfWork(ctx))
                {
                    await inner.BeginTransactionAsync(TestContext.Current.CancellationToken);
                    await ctx.Organizations.AddAsync(Any.Organization(), TestContext.Current.CancellationToken);
                    await inner.CommitAsync(TestContext.Current.CancellationToken);
                }

                throw new Exception("simulated-failure");
            }
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);

        await using var verify = new ApplicationDbContext(_options);
        Assert.Empty(verify.Organizations.ToList());
    }
}
