using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.IntegrationTests.Factories;
using API.Models;
using API.Services.ConnectionInvitationCleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Services;

public class ConnectionInvitationCleanupServiceTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly Guid sub;
    private readonly string tin;

    public ConnectionInvitationCleanupServiceTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid();
        tin = "11223344";
        factory.WalletUrl = "UnusedWalletUrl";
    }

    [Fact]
    public async Task Run_ShouldDeleteOldInvitations_WhenInvoked()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IConnectionInvitationCleanupService>();

        dbContext.ConnectionInvitations.RemoveRange(dbContext.ConnectionInvitations);
        await dbContext.SaveChangesAsync();

        var newInvitation = new ConnectionInvitation
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = sub,
            SenderCompanyTin = tin,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var oldInvitation = new ConnectionInvitation
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = sub,
            SenderCompanyTin = tin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14)
        };

        dbContext.ConnectionInvitations.Add(newInvitation);
        dbContext.ConnectionInvitations.Add(oldInvitation);
        await dbContext.SaveChangesAsync();

        var cancellationTokenSource = new CancellationTokenSource();

        var runTask = cleanupService.Run(cancellationTokenSource.Token);

        await Task.Delay(100);

        cancellationTokenSource.Cancel();
        await runTask;

        var remainingInvitations = await dbContext.ConnectionInvitations.CountAsync();
        Assert.Equal(1, remainingInvitations);
    }
}
