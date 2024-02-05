using System;
using System.Linq;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.v2024_01_03.Dto.Requests;
using EnergyOrigin.ActivityLog.API;
using Xunit;
using Xunit.Abstractions;

namespace API.IntegrationTests.Transfer.Api.v2024_01_03;

public class TransferAgreementsControllerTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly ITestOutputHelper output;
    private readonly Api api;

    public TransferAgreementsControllerTest(TransferAgreementsApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        this.api = new Api(factory, output);
    }

    [Fact]
    public async Task GivenProposal_WhenAcceptingProposal_ActivityLogEntryIsAdded()
    {
        var receiverTin = "12334455";

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
}
