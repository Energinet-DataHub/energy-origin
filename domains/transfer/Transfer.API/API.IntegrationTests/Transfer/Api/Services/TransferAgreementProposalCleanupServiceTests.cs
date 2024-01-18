using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using DataContext;
using DataContext.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
}
