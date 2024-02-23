using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.v2024_01_03.Dto.Requests;
using EnergyOrigin.ActivityLog.API;
using Xunit;
using Xunit.Abstractions;

namespace API.IntegrationTests.Transfer.Api.v2024_01_03;

public class TransferAgreementsControllerTest(ITestOutputHelper output)
    : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    [Fact]
    public async Task GivenProposal_WhenAcceptingProposal_ActivityLogEntryIsAdded()
    {
        var receiverTin = "12334455";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);
        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, createdProposalId);

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Equal(2, senderLog.ActivityLogEntries.Count());
        Assert.Single(receiverLog.ActivityLogEntries);
    }

    [Fact]
    public async Task GivenAgreement_WhenChangingEndDate_ActivityLogEntryIsAdded()
    {
        var receiverTin = "12334455";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);
        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        var agreementId = await api.AcceptTransferAgreementProposal(receiverTin, createdProposalId);

        await api.EditEndDate(agreementId, DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds());

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Equal(3, senderLog.ActivityLogEntries.Count());
        Assert.Equal(2, receiverLog.ActivityLogEntries.Count());
    }

    [Fact]
    public async Task GivenProposal_WhenAcceptingProposal_ActivityLogEntryIsCleanedUp()
    {
        var receiverTin = "12334456";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = true;
        await factory.InitializeAsync();
        var api = new Api(factory, output);
        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, createdProposalId);

        await Task.Delay(3200);

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Empty(senderLog.ActivityLogEntries);
        Assert.Empty(receiverLog.ActivityLogEntries);
    }
}
