using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using DataContext;
using DataContext.Models;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Services;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementProposalCleanupServiceTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly Guid sub;
    private readonly Tin tin;

    public TransferAgreementProposalCleanupServiceTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
        sub = Guid.NewGuid();
        tin = Tin.Create("11223344");
        factory.CreateClient();
    }

    [Fact]
    public async Task Run_ShouldDeleteOldInvitations_WhenInvoked()
    {
        using var scope = factory.Services.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.TruncateTableAsync<TransferAgreementProposal>();

        var newInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = OrganizationId.Create(sub),
            SenderCompanyTin = tin,
            CreatedAt = UnixTimestamp.Now(),
            ReceiverCompanyTin = Tin.Create("12345678"),
            EndDate = UnixTimestamp.Now().AddDays(1),
            StartDate = UnixTimestamp.Now(),
            SenderCompanyName = OrganizationName.Create("SomeCompany")
        };

        var oldInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = OrganizationId.Create(sub),
            SenderCompanyTin = tin,
            CreatedAt = UnixTimestamp.Now().AddDays(-14),
            ReceiverCompanyTin = Tin.Create("12345678"),
            EndDate = UnixTimestamp.Now().AddDays(1),
            StartDate = UnixTimestamp.Now().AddDays(-14),
            SenderCompanyName = OrganizationName.Create("SomeCompany")
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
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.TruncateTableAsync<ActivityLogEntry>();
        await dbContext.TruncateTableAsync<TransferAgreementProposal>();

        var oldInvitation = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = OrganizationId.Create(sub),
            SenderCompanyTin = tin,
            CreatedAt = UnixTimestamp.Now().AddDays(-14),
            ReceiverCompanyTin = Tin.Create("12345678"),
            EndDate = UnixTimestamp.Now().AddDays(1),
            StartDate = UnixTimestamp.Now().AddDays(-14),
            SenderCompanyName = OrganizationName.Create("SomeCompany")
        };

        dbContext.TransferAgreementProposals.Add(oldInvitation);
        await dbContext.SaveChangesAsync();

        var invitations = await dbContext.RepeatedlyQueryUntilCountIsMet<TransferAgreementProposal>(0, TimeSpan.FromSeconds(30));

        invitations.Should().BeEmpty();
        var client = factory.CreateAuthenticatedClient(sub.ToString(), tin: tin.Value);

        var post = await client.PostAsJsonAsync("api/transfer/activity-log", new ActivityLogEntryFilterRequest(null, null, null));
        post.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLogResponseBody = await post.Content.ReadAsStringAsync();
        var logs = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(activityLogResponseBody);
        logs!.ActivityLogEntries.Should().ContainSingle();
    }
}
