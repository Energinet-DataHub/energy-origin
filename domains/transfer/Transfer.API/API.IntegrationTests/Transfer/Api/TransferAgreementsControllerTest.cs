using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using EnergyOrigin.ActivityLog.API;
using Xunit;
using Xunit.Abstractions;

namespace API.IntegrationTests.Transfer.Api;

public class TransferAgreementsControllerTest(ITestOutputHelper output)
    : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    [Fact]
    public async Task GivenProposal_WhenAcceptingProposal_ActivityLogEntryIsAdded()
    {
        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, receiverName, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Equal(2, senderLog.ActivityLogEntries.Count());
        Assert.Single(receiverLog.ActivityLogEntries);
    }

    [Fact]
    public async Task GivenProposal_WhenAcceptedAndLoggedInAsReceiver_ActivityLogIncludesOrganizationDetailsForBothCompanies()
    {
        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);
        var receiverClient = api.MockWalletServiceAndCreateAuthenticatedClient(receiverTin, receiverName);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        // Assert activity log contains the required organization details
        var receiverLog = await api.GetActivityLog(receiverClient, new ActivityLogEntryFilterRequest(null, null, null));

        var proposalLogEntryForReceiver = receiverLog.ActivityLogEntries.First();

            Assert.Equal(receiverTin, proposalLogEntryForReceiver.OrganizationTin);
            Assert.Equal(receiverName, proposalLogEntryForReceiver.OrganizationName);
            Assert.Equal("11223344", proposalLogEntryForReceiver.OtherOrganizationTin);
            Assert.Equal("Producent A/S", proposalLogEntryForReceiver.OtherOrganizationName);
    }

    [Fact]
    public async Task IfProposalAccepted_WhenAndLoggedInAsSender_ActivityLogIncludesOrganizationDetailsForBothCompanies()
    {
        var senderCompanyTin = "11223344";
        var senderCompanyName = "Producent A/S";

        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);
        var senderClient = api.MockWalletServiceAndCreateAuthenticatedClient(senderCompanyTin, senderCompanyName);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        var senderLog = await api.GetActivityLog(senderClient, new ActivityLogEntryFilterRequest(null, null, null));

        var proposalLogEntryForSender = senderLog.ActivityLogEntries.Last();

        Assert.Equal("11223344", proposalLogEntryForSender.OrganizationTin);
        Assert.Equal("Producent A/S", proposalLogEntryForSender.OrganizationName);
        Assert.Equal(receiverTin, proposalLogEntryForSender.OtherOrganizationTin);
        Assert.Equal(receiverName, proposalLogEntryForSender.OtherOrganizationName);
    }

    [Fact]
    public async Task GivenProposalByPeter_WhenAcceptedByNrgi_ActivityLogReflectsCorrectDetails()
    {
        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, receiverName, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Equal(2, senderLog.ActivityLogEntries.Count());
        Assert.Single(receiverLog.ActivityLogEntries);
    }

    [Fact]
    public async Task GivenAgreement_WhenChangingEndDate_ActivityLogEntryIsAdded()
    {
        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = false;
        await factory.InitializeAsync();
        var api = new Api(factory, output);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        var agreementId = await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        await api.EditEndDate(agreementId, DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds());

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, receiverName, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Equal(3, senderLog.ActivityLogEntries.Count());
        Assert.Equal(2, receiverLog.ActivityLogEntries.Count());
    }

    [Fact]
    public async Task GivenProposal_WhenAcceptingProposal_ActivityLogEntryIsCleanedUp()
    {
        var receiverTin = "39293595";
        var receiverName = "Company Inc.";

        var factory = new TransferAgreementsApiWebApplicationFactory();
        factory.WithCleanupWorker = true;
        await factory.InitializeAsync();
        var api = new Api(factory, output);

        // Create transfer agreement proposal
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await api.CreateTransferAgreementProposal(request);

        // Accept proposal
        await api.AcceptTransferAgreementProposal(receiverTin, receiverName, createdProposalId);

        await Task.Delay(3200);

        // Assert activity was logged
        var senderLog = await api.GetActivityLog(new ActivityLogEntryFilterRequest(null, null, null));
        var receiverLog = await api.GetActivityLog(receiverTin, receiverName, new ActivityLogEntryFilterRequest(null, null, null));
        Assert.Empty(senderLog.ActivityLogEntries);
        Assert.Empty(receiverLog.ActivityLogEntries);
    }
}
