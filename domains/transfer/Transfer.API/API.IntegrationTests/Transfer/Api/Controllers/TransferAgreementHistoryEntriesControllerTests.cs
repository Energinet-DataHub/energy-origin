using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Shared;
using API.IntegrationTests.Testcontainers;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.Transfer.Api.Models;
using FluentAssertions;
using Newtonsoft.Json;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[UsesVerify]
public class TransferAgreementHistoryEntriesControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>, IClassFixture<WalletContainer>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;

    public TransferAgreementHistoryEntriesControllerTests(TransferAgreementsApiWebApplicationFactory factory,
        WalletContainer wallet)
    {
        this.factory = factory;
        factory.WalletUrl = wallet.WalletUrl;
        sub = Guid.NewGuid().ToString();
    }

    [Fact]
    public async Task Create_ShouldGenerateHistoryEntry_WhenTransferAgreementIsCreated()
    {
        var senderClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12334455";
        var body = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", body);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposal!.Id));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var auditsResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement!.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task Edit_ShouldGenerateHistoryEntry_WhenEndDateIsUpdated()
    {
        var senderClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12334455";
        var createProposal = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", createProposal);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);
        var createResponse = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposal!.Id));
        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newEndDate = new DateTimeOffset(2125, 5, 5, 5, 5, 5, TimeSpan.Zero).ToUnixTimeSeconds();
        var editRequest = new EditTransferAgreementEndDate(newEndDate);
        await receiverClient.PatchAsync($"api/transfer-agreements/{createdTransferAgreement!.Id}", JsonContent.Create(editRequest));

        var auditsResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(auditsResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldReturnNoContent_WhenNotOwnerOrReceiver()
    {
        var senderClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12334455";
        var createProposal = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", createProposal);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposal!.Id));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "66332211");
        var response = await newAuthenticatedClient.GetAsync($"api/history/transfer-agreements/{createdTransferAgreement!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldContainActorNameField_WhenSender()
    {
        var senderClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12334455";
        var createProposal = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", createProposal);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposal!.Id));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var otherActor = Guid.NewGuid();
        var otherActorClient = factory.CreateAuthenticatedClient(sub: sub, actor: otherActor.ToString());

        var senderResponse = await otherActorClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement!.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(senderResponse, settings);
    }

    [Fact]
    public async Task GetHistoryEntriesForTransferAgreement_ShouldNotContainActorNameField_WhenReceiver()
    {
        var senderClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12334455";
        var createProposal = new CreateTransferAgreementProposal(
            new DateTimeOffset(2123, 3, 3, 3, 3, 3, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2124, 4, 4, 4, 4, 4, TimeSpan.Zero).ToUnixTimeSeconds(),
            receiverTin
        );
        var result = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", createProposal);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString(), tin: receiverTin);
        var createRequest = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposal!.Id));
        var createdTransferAgreement = await createRequest.Content.ReadFromJsonAsync<TransferAgreementDto>();

        var receiverResponse = await receiverClient.GetFromJsonAsync<TransferAgreementHistoryEntriesResponse>
            ($"api/history/transfer-agreements/{createdTransferAgreement!.Id}", JsonDefault.Options);

        var settings = new VerifySettings();
        settings.ScrubMember("CreatedAt");

        await Verifier.Verify(receiverResponse, settings);
    }
}
