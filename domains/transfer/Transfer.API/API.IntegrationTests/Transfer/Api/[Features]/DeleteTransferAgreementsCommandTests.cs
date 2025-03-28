using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using EnergyOrigin.Setup.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using API.UnitTests;
using API.Transfer.Api._Features_;

namespace API.IntegrationTests.Transfer.Api._Features_;

[Collection(IntegrationTestCollection.CollectionName)]
public class DeleteTransferAgreementsCommandTests
{
    private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

    public DeleteTransferAgreementsCommandTests(IntegrationTestFixture integrationTestFixture)
    {
        var databaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(databaseInfo.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
        _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(databaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task GivenCommand_WhenOrgIsEitherSenderAndOrReceiver_DeleteTransferAgreements()
    {
        var cancellationToken = new CancellationToken();
        var orgId = Any.OrganizationId();
        var ta1 = Any.TransferAgreement(senderId: orgId, receiverId: Any.OrganizationId());
        var ta2 = Any.TransferAgreement(senderId: Any.OrganizationId(), receiverId: orgId);
        var ta3 = Any.TransferAgreement(senderId: Any.OrganizationId(), Any.OrganizationId());

        await using var dbContext = new ApplicationDbContext(_contextOptions);
        dbContext.TransferAgreements.Add(ta1);
        dbContext.TransferAgreements.Add(ta2);
        dbContext.TransferAgreements.Add(ta3);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sut = new DeleteTransferAgreementsCommandHandler(dbContext);
        var cmd = new DeleteTransferAgreementsCommand(orgId);

        await sut.Handle(cmd, cancellationToken);

        var tasDb = dbContext.TransferAgreements.ToList();

        var shouldBeDeleted = tasDb.Where(x =>
            (x.ReceiverId != null && x.ReceiverId == cmd.OrganizationId) || x.SenderId == cmd.OrganizationId).ToList();

        Assert.Empty(shouldBeDeleted);
        Assert.Contains(ta3, tasDb);
    }
}
