using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementAuditsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;

    public TransferAgreementAuditsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Create_ShouldGenerateAudit_WhenTransferAgreementIsCreated()
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var transferAgreement = new CreateTransferAgreement(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            "12345678"
        );

        var createResponse = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        createResponse.EnsureSuccessStatusCode();

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdTransferAgreement = await createResponse.Content.ReadFromJsonAsync<TransferAgreementDto>();
        createdTransferAgreement.Should().NotBeNull();

        var auditsResponse = await authenticatedClient.GetAsync($"api/audits/transfer-agreements/{createdTransferAgreement.Id}");
        auditsResponse.EnsureSuccessStatusCode();

        auditsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var audits = await auditsResponse.Content.ReadFromJsonAsync<TransferAgreementAuditsResponse>();
        audits.Should().NotBeNull();
        audits.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Edit_ShouldGenerateAudit_WhenTransferAgreementEndDateIsChanged()
    {
        var senderId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = agreementId,
                SenderId = senderId,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                ActorId = "actor1",
                ActorName = "Thomas Tesla",
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "1122334"
            }
        });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();
        var request = new EditTransferAgreementEndDate(newEndDate);

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{agreementId}", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        updatedTransferAgreement.Should().NotBeNull();
        updatedTransferAgreement.EndDate.Should().Be(newEndDate);

        var auditsResponse = await authenticatedClient.GetAsync($"api/audits/transfer-agreements/{agreementId}");
        auditsResponse.EnsureSuccessStatusCode();

        auditsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var audits = await auditsResponse.Content.ReadFromJsonAsync<TransferAgreementAuditsResponse>();
        audits.Should().NotBeNull();
        audits.Result.Should().HaveCount(2);
        audits.Result.Last().EndDate.Should().Be(newEndDate);
    }

    [Fact]
    public async Task GetAuditsForTransferAgreement_ReturnsNoContent_WhenNotOwnerOrReceiver()
    {
        var transferAgreementId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var receiverTin = "12345678";

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                ActorId = "actor1",
                ActorName = "Peter Producent",
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin
            }
        });

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "87654321");

        var response = await newAuthenticatedClient.GetAsync($"api/audits/transfer-agreements/{transferAgreementId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAuditsForTransferAgreement_ReturnsTransferAgreementAuditsResponse_WithCorrectDtoFormat(bool isSender)
    {
        var transferAgreementId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var senderTin = "11223344";
        var receiverId = Guid.NewGuid().ToString();
        var receiverTin = "44332211";

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                ActorId = "actor1",
                ActorName = "Peter Producent",
                SenderId = senderId,
                SenderName = "Producent A/S",
                SenderTin = "11223344",
                ReceiverTin = receiverTin
            }
        });

        var senderClient = factory.CreateAuthenticatedClient(sub: senderId.ToString(), tin: senderTin);
        var receiverClient = factory.CreateAuthenticatedClient(sub: receiverId, tin: receiverTin);

        var response = await (isSender ? senderClient : receiverClient).GetAsync($"api/audits/transfer-agreements/{transferAgreementId}");

        var auditsResponse = await response.Content.ReadFromJsonAsync<TransferAgreementAuditsResponse>();
        auditsResponse.Should().NotBeNull();
        auditsResponse.Result.Should().HaveCount(1);

        var auditDto = auditsResponse.Result.First();
        auditDto.SenderName.Should().Be("Producent A/S");
        auditDto.SenderTin.Should().Be(senderTin);
        auditDto.ReceiverTin.Should().Be(receiverTin);

        if (isSender)
        {
            auditDto.ActorName.Should().Be("Peter Producent");
        }
        else
        {
            auditDto.ActorName.Should().BeNull();
        }
    }
}
