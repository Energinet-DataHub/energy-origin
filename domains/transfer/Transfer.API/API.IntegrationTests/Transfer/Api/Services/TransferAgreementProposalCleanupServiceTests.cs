using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using DataContext;
using DataContext.Models;
using EnergyOrigin.ActivityLog.API;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Services;

public class TransferAgreementProposalCleanupServiceTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly Guid sub;
    private readonly string tin;

    public TransferAgreementProposalCleanupServiceTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid();
        tin = "11223344";
        factory.CreateClient();
    }

    [Fact]
    public async Task Run_ShouldDeleteOldInvitations_WhenInvoked()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        dbContext.TransferAgreementProposals.RemoveRange(dbContext.TransferAgreementProposals);
        await dbContext.SaveChangesAsync();

        var newInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = sub,
            SenderCompanyTin = tin,
            CreatedAt = DateTimeOffset.UtcNow,
            ReceiverCompanyTin = "12345678",
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            StartDate = DateTimeOffset.UtcNow,
            SenderCompanyName = "SomeCompany"
        };

        var oldInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = sub,
            SenderCompanyTin = tin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14),
            ReceiverCompanyTin = "12345678",
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-14),
            SenderCompanyName = "SomeCompany"
        };

        dbContext.TransferAgreementProposals.Add(newInvitation);
        dbContext.TransferAgreementProposals.Add(oldInvitation);
        await dbContext.SaveChangesAsync();

        var invitations = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreementProposal>(1);

        invitations.FirstOrDefault()!.Id.Should().Be(newInvitation.Id);
    }

    [Fact]
    public async Task ShouldCreateActivityLogEntry_WhenDeleting()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransferDbContext>();

        dbContext.TransferAgreementProposals.RemoveRange(dbContext.TransferAgreementProposals);
        await dbContext.SaveChangesAsync();

        var oldInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = sub,
            SenderCompanyTin = tin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14),
            ReceiverCompanyTin = "12345678",
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-14),
            SenderCompanyName = "SomeCompany"
        };

        dbContext.TransferAgreementProposals.Add(oldInvitation);
        await dbContext.SaveChangesAsync();

        var invitations = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreementProposal>(0, TimeSpan.FromSeconds(30));

        invitations.Should().BeEmpty();
        var client = factory.CreateAuthenticatedClient(sub.ToString(), tin: tin);

        var post = await client.PostAsJsonAsync("api/transfer/activity-log", new ActivityLogEntryFilterRequest(null, null, null));
        post.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLogResponseBody = await post.Content.ReadAsStringAsync();
        var logs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(activityLogResponseBody);
        logs!.ActivityLogEntries.Should().ContainSingle();
    }
}
