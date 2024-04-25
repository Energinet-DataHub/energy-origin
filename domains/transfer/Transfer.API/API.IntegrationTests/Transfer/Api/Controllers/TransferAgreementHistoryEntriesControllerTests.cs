using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Shared;
using API.Transfer.Api.Controllers;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOriginClients;
using ProjectOriginClients.Models;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

public class TransferAgreementHistoryEntriesControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;
    private readonly HttpClient senderClient;

    public TransferAgreementHistoryEntriesControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();
        senderClient = factory.CreateAuthenticatedClient(sub, apiVersion: ApiVersions.Version20240103);
    }

    [Fact]
    public async Task Create_ShouldGenerateHistoryEntry_WhenTransferAgreementIsCreated()
    {
        var receiverTin = "12334455";
        var createdProposalId = await CreateTransferAgreementProposal(receiverTin);

        var poWalletServiceMock = SetupPoWalletServiceMock();

        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", new CreateTransferAgreement(createdProposalId));
        createRequest.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var auditsResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/transfer/transfer-agreements/{createdTransferAgreement!.Id}/history?offset=0&limit=1", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task Edit_ShouldGenerateHistoryEntry_WhenEndDateIsUpdated()
    {
        var receiverTin = "12334455";

        var createdProposalId = await CreateTransferAgreementProposal(receiverTin);

        var poWalletServiceMock = SetupPoWalletServiceMock();

        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, Guid.NewGuid().ToString(), tin: receiverTin);
        var createResponse = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", new CreateTransferAgreement(createdProposalId));
        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newEndDate = new DateTimeOffset(2125, 5, 5, 5, 5, 5, TimeSpan.Zero).ToUnixTimeSeconds();
        var editRequest = new EditTransferAgreementEndDate(newEndDate);
        await senderClient.PatchAsync($"api/transfer/transfer-agreements/{createdTransferAgreement!.Id}", JsonContent.Create(editRequest));

        var auditsResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/transfer/transfer-agreements/{createdTransferAgreement.Id}/history?offset=0&limit=1", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldReturnNoContent_WhenNotOwnerOrReceiver()
    {
        var receiverTin = "12334455";
        var createdProposalId = await CreateTransferAgreementProposal(receiverTin);
        var poWalletServiceMock = SetupPoWalletServiceMock();

        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", new CreateTransferAgreement(createdProposalId));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "66332211");
        var response = await newAuthenticatedClient.GetAsync($"api/transfer/transfer-agreements/{createdTransferAgreement!.Id}/history?offset=0&limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldContainActorNameField_WhenSender()
    {
        var receiverTin = "12334455";
        var createdProposalId = await CreateTransferAgreementProposal(receiverTin);
        var poWalletServiceMock = SetupPoWalletServiceMock();

        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", new CreateTransferAgreement(createdProposalId));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var otherActor = Guid.NewGuid();
        var otherActorClient = factory.CreateAuthenticatedClient(sub: sub, actor: otherActor.ToString());

        var senderResponse = await otherActorClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/transfer/transfer-agreements/{createdTransferAgreement!.Id}/history?offset=0&limit=1", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(senderResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldNotContainActorNameField_WhenReceiver()
    {
        var receiverTin = "12334455";
        var createdProposalId = await CreateTransferAgreementProposal(receiverTin);
        var poWalletServiceMock = SetupPoWalletServiceMock();

        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", new CreateTransferAgreement(createdProposalId));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var receiverResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/transfer/transfer-agreements/{createdTransferAgreement!.Id}/history?offset=0&limit=1", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(receiverResponse, settings);
    }

    private async Task<Guid> CreateTransferAgreementProposal(string receiverTin)
    {
        var body = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer/transfer-agreement-proposals", body);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        return createdProposal!.Id;
    }

    private IProjectOriginWalletClient SetupPoWalletServiceMock()
    {
        var walletClientMock = Substitute.For<IProjectOriginWalletClient>();
        walletClientMock.CreateWallet(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        walletClientMock.GetWallets(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            new ResultList<WalletRecord>
            {
                Metadata = new PageInfo { Count = 1, Limit = 100, Total = 1, Offset = 0 },
                Result = new List<WalletRecord>
                {
                    new WalletRecord { Id = Guid.NewGuid(), PublicKey = new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter() }
                }
            });
        walletClientMock.CreateWalletEndpoint(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new WalletEndpointReference(1, new Uri("http://someUrl"), new Secp256k1Algorithm().GenerateNewPrivateKey().Neuter()));
        walletClientMock.CreateExternalEndpoint(Arg.Any<Guid>(), Arg.Any<WalletEndpointReference>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new CreateExternalEndpointResponse { ReceiverId = Guid.NewGuid() });

        return walletClientMock;
    }
}
