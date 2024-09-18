using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using DataContext;
using DataContext.Models;
using EnergyOrigin.ActivityLog.API;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementProposalsControllerTests
{
    private readonly Guid sub = Guid.NewGuid();
    private readonly Guid orgId = Guid.NewGuid();
    private readonly string tin = "12345678";

    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public TransferAgreementProposalsControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.Factory;
    }

    [Fact]
    public async Task Create()
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var body = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, "12334455");
        var result = await authenticatedClient
            .PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", body);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = factory.CreateUnauthenticatedClient();
        var result = await client.PostAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", null);

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(-1, null, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(-1, 4, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(3, 1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    [InlineData(0, -1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    public async Task Create_ShouldFail_WhenStartOrEndDateInvalid(int startDayOffset, int? endDayOffset, HttpStatusCode expectedStatusCode,
        string expectedContent)
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var now = DateTimeOffset.UtcNow;

        var startDate = now.AddDays(startDayOffset).ToUnixTimeSeconds();
        var endDate = endDayOffset.HasValue ? now.AddDays(endDayOffset.Value).ToUnixTimeSeconds() : (long?)null;

        var request = new CreateTransferAgreementProposal(
            StartDate: startDate,
            EndDate: endDate,
            ReceiverTin: "12345678"
        );

        var response = await authenticatedClient.PostAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}",
            JsonContent.Create(request));

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(expectedStatusCode);
        validationProblemContent.Should().Contain(expectedContent);
    }

    [Theory]
    [InlineData(253402300800L, null, "StartDate")]
    [InlineData(221860025546L, 253402300800L, "EndDate")]
    public async Task Create_ShouldFail_WhenDateInvalid(long start, long? end, string property)
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var receiverTin = "12345678";

        var request = new CreateTransferAgreementProposal(
            StartDate: start,
            EndDate: end,
            ReceiverTin: receiverTin
        );

        var response = await authenticatedClient.PostAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}",
            JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain("too high! Please make sure the format is UTC in seconds.");
        validationProblemContent.Should().Contain(property);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartDateOrEndDateCauseOverlap()
    {
        var senderTin = "12345678";
        var receiverTin = "88776655";
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId, senderTin);
        var id = Guid.NewGuid();
        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = orgId,
                SenderName = "nrgi A/S",
                SenderTin = senderTin,
                ReceiverTin = receiverTin,
                ReceiverReference = Guid.NewGuid()
            }
        });

        var overlappingRequest = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds(),
            ReceiverTin: receiverTin
        );

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}",
            overlappingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldSucceed_WhenStartDateOrEndDateCauseOverlapAndReceiverTinIsNull()
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var id = Guid.NewGuid();
        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = orgId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid()
            }
        });

        var overlappingRequest = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds(),
            ReceiverTin: null
        );

        var response = await authenticatedClient.PostAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}",
            JsonContent.Create(overlappingRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GivenProposalToOwnCvr_WhenCreatingProposal_ValidationErrorIsReturned()
    {
        var cvr = "11223355";
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId, cvr);
        var request = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            ReceiverTin: cvr
        );

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain("ReceiverTin cannot be the same as SenderTin.");
        validationProblemContent.Should().Contain("ReceiverTin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12345678")]
    public async Task Create_ShouldSucceed_WhenReceiverTinValid(string? receiverTin)
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var request = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            ReceiverTin: receiverTin
        );

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnOK_WhenInvitationExists()
    {
        var receiverTin = "11223345";
        var senderClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);

        var createResponse = await senderClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        var receiverOrgId = Guid.NewGuid();
        var receiverClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), receiverOrgId, receiverTin);

        var getResponse =
            await receiverClient.GetAsync($"api/transfer/transfer-agreement-proposals/{createdProposal!.Id}?organizationId={receiverOrgId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();
        var returnedProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(getResponseBody);

        returnedProposal.Should().BeEquivalentTo(createdProposal);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenCurrentUserIsSender()
    {
        var receiverTin = "11223345";
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);
        var createResponse = await client.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        var response = await client.GetAsync($"api/transfer/transfer-agreement-proposals/{createdProposal!.Id}?organizationId={orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenCurrentUserIsNotTheIntendedForTheProposal()
    {
        var receiverTin = "11223345";
        var senderClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);

        var createResponse = await senderClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        var receiverOrgId = Guid.NewGuid();
        var receiverClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), receiverOrgId, "12345679");

        var getResponse =
            await receiverClient.GetAsync($"api/transfer/transfer-agreement-proposals/{createdProposal!.Id}?organizationId={receiverOrgId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnBadRequest_WhenProposalHasRunOut()
    {
        var receiverTin = "11223345";

        var taProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            Id = Guid.NewGuid(),
            SenderCompanyName = "SomeCompany",
            ReceiverCompanyTin = receiverTin,
            SenderCompanyId = orgId,
            SenderCompanyTin = "11223344"
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { taProposal });

        var receiverClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), orgId, receiverTin);

        var getResponse = await receiverClient.GetAsync($"api/transfer/transfer-agreement-proposals/{taProposal.Id}?organizationId={orgId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnNotFound_WhenInvitationDoesNotExist()
    {
        var nonExistentInvitationId = Guid.NewGuid();
        var client = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var response = await client.GetAsync($"api/transfer/transfer-agreement-proposals/{nonExistentInvitationId}?organizationId={orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task GetTransferAgreementProposal_ShouldReturnOk_WhenProposalWithReceiverTinIsNull()
    {
        var taProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            Id = Guid.NewGuid(),
            SenderCompanyName = "SomeCompany",
            ReceiverCompanyTin = null,
            SenderCompanyId = Guid.NewGuid(),
            SenderCompanyTin = "11223344"
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { taProposal });

        var client = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var response = await client.GetAsync($"api/transfer/transfer-agreement-proposals/{taProposal.Id}?organizationId={orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { proposal });

        var client = factory.CreateB2CAuthenticatedClient(sub, orgId, tin);

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(response => response.StatusCode != HttpStatusCode.NotFound)
            .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(1));

        var response = await retryPolicy.ExecuteAsync(
            () => client.GetAsync($"api/transfer/transfer-agreement-proposals/{invitationId}?organizationId={orgId}"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_OnSuccessfulDelete()
    {
        var receiverTin = "32132132";
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), receiverTin);
        var postResponse = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProposal = await postResponse.Content.ReadFromJsonAsync<TransferAgreementProposalResponse>();

        var receiverOrgId = Guid.NewGuid();
        var receiverClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), receiverOrgId, receiverTin);

        var deleteResponse =
            await receiverClient.DeleteAsync($"api/transfer/transfer-agreement-proposals/{createdProposal!.Id}?organizationId={receiverOrgId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenConnectionInvitationNonExisting()
    {
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var randomGuid = Guid.NewGuid();

        var deleteResponse = await authenticatedClient.DeleteAsync($"api/transfer/transfer-agreement-proposals/{randomGuid}?organizationId={orgId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenActivityLog_WhenUsingB2CAuth_EndpointReturnsOk()
    {
        var client = factory.CreateB2CAuthenticatedClient(sub: Guid.NewGuid(), orgId: Guid.NewGuid(), tin: "1223344");
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/transfer/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SeedTransferAgreements(List<TransferAgreement> transferAgreements)
    {
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        await TestData.SeedTransferAgreements(dbContext, transferAgreements);
    }

    private async Task SeedTransferAgreementProposals(List<TransferAgreementProposal> transferAgreementProposals)
    {
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        await TestData.SeedTransferAgreementProposals(dbContext, transferAgreementProposals);
    }
}
