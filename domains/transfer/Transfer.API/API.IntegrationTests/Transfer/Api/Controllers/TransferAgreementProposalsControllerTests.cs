using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

public class TransferAgreementProposalsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly string sub;
    private readonly string tin;

    public TransferAgreementProposalsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        sub = Guid.NewGuid().ToString();
        tin = "12345678";
    }

    [Fact]
    public async Task Create()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var body = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, "12334455");
        var result = await authenticatedClient
            .PostAsJsonAsync("api/transfer-agreement-proposals", body);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.PostAsync("api/transfer-agreement-proposals", null);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(-1, null, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(-1, 4, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(3, 1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    [InlineData(0, -1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    public async Task Create_ShouldFail_WhenStartOrEndDateInvalid(int startDayOffset, int? endDayOffset, HttpStatusCode expectedStatusCode, string expectedContent)
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var now = DateTimeOffset.UtcNow;

        var startDate = now.AddDays(startDayOffset).ToUnixTimeSeconds();
        var endDate = endDayOffset.HasValue ? now.AddDays(endDayOffset.Value).ToUnixTimeSeconds() : (long?)null;

        var request = new CreateTransferAgreementProposal(
            StartDate: startDate,
            EndDate: endDate,
            ReceiverTin: "12345678"
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreement-proposals", JsonContent.Create(request));

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(expectedStatusCode);
        validationProblemContent.Should().Contain(expectedContent);
    }

    [Theory]
    [InlineData(253402300800L, null, "StartDate")]
    [InlineData(221860025546L, 253402300800L, "EndDate")]
    public async Task Create_ShouldFail_WhenDateInvalid(long start, long? end, string property)
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var receiverTin = "12345678";

        var request = new CreateTransferAgreementProposal(
            StartDate: start,
            EndDate: end,
            ReceiverTin: receiverTin
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreement-proposals", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain("too high! Please make sure the format is UTC in seconds.");
        validationProblemContent.Should().Contain(property);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartDateOrEndDateCauseOverlap()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var id = Guid.NewGuid();
        await factory.SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = Guid.Parse(sub),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid()
            }
        });

        var overlappingRequest = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds(),
            ReceiverTin: "12345678"
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreement-proposals", JsonContent.Create(overlappingRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "ReceiverTin cannot be empty")]
    [InlineData("1234567", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("123456789", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("ABCDEFG", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("11223344", "ReceiverTin cannot be the same as SenderTin.")]
    public async Task Create_ShouldFail_WhenReceiverTinInvalid(string tin, string expectedContent)
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var request = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            ReceiverTin: tin
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreement-proposals", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain(expectedContent);
        validationProblemContent.Should().Contain("ReceiverTin");
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnOK_WhenInvitationExists()
    {
        var receiverTin = "11223345";
        var senderClient = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);

        var createResponse = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: receiverTin);

        var getResponse = await receiverClient.GetAsync($"api/transfer-agreement-proposals/{createdProposal!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();
        var returnedProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(getResponseBody);

        returnedProposal.Should().BeEquivalentTo(createdProposal);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenCurrentUserIsSender()
    {
        var receiverTin = "11223345";
        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);
        var createResponse = await client.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var response = await client.GetAsync($"api/transfer-agreement-proposals/{createdProposal!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();

        responseBody.Should().Be("You cannot Accept/Deny your own TransferAgreementProposal");
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenCurrentUserIsNotTheIntendedForTheProposal()
    {
        var receiverTin = "11223345";
        var senderClient = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);

        var createResponse = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: "12345679");

        var getResponse = await receiverClient.GetAsync($"api/transfer-agreement-proposals/{createdProposal!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();

        getResponseBody.Should().Be("You cannot Accept/Deny a TransferAgreementProposal for another company");
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenProposalHasRunOut()
    {
        var receiverTin = "11223345";
        var senderClient = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), receiverTin);

        var createResponse = await senderClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        var receiverClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: receiverTin);

        var getResponse = await receiverClient.GetAsync($"api/transfer-agreement-proposals/{createdProposal!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();

        getResponseBody.Should().Be("This proposal has run out");
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnNotFound_WhenInvitationDoesNotExist()
    {
        var nonExistentInvitationId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/transfer-agreement-proposals/{nonExistentInvitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("TransferAgreementProposal expired or deleted");
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnNotFound_WhenTransferAgreementProposalExpired()
    {
        var companyId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        var proposal = new TransferAgreementProposal
        {
            Id = invitationId,
            SenderCompanyId = companyId,
            SenderCompanyTin = "32132132",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-14),
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-14),
            SenderCompanyName = "SomeCompany",
            ReceiverCompanyTin = tin
        };

        await factory.SeedTransferAgreementProposals(new List<TransferAgreementProposal> { proposal });

        var client = factory.CreateAuthenticatedClient(sub: sub, tin: tin);

        var response = await client.GetAsync($"api/transfer-agreement-proposals/{invitationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("TransferAgreementProposal expired or deleted");
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_OnSuccessfulDelete()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "32132132");
        var postResponse = await authenticatedClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);

        var createdProposal = await postResponse.Content.ReadFromJsonAsync<TransferAgreementProposal>();

        var deleteResponse = await authenticatedClient.DeleteAsync($"api/transfer-agreement-proposals/{createdProposal!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenConnectionInvitationNonExisting()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);
        var randomGuid = Guid.NewGuid();

        var deleteResponse = await authenticatedClient.DeleteAsync($"api/transfer-agreement-proposals/{randomGuid}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
