using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.Setup;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace API.IntegrationTests.Transfer.Api;

public class Api
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly ITestOutputHelper output;
    private readonly string sub;
    private readonly string tin;
    private readonly string cpn;
    private readonly HttpClient authenticatedClient;
    private readonly string apiVersion = ApiVersions.Version1;

    public Api(TransferAgreementsApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        sub = Guid.NewGuid().ToString();
        tin = "11223344";
        cpn = "Producent A/S";
        authenticatedClient = factory.CreateAuthenticatedClient(sub, tin, cpn, apiVersion: apiVersion);
    }

    public async Task<Guid> CreateTransferAgreementProposal(CreateTransferAgreementProposal request)
    {
        var result = await authenticatedClient.PostAsJsonAsync("api/transfer/transfer-agreement-proposals", request);
        output.WriteLine(await result.Content.ReadAsStringAsync());
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        return createdProposal!.Id;
    }

    public async Task<Guid> AcceptTransferAgreementProposal(string receiverTin, string receiverName, Guid createdProposalId)
    {
        var receiverClient = MockWalletServiceAndCreateAuthenticatedClient(receiverTin, receiverName);
        var transferAgreement = new CreateTransferAgreement(createdProposalId);

        var response = await receiverClient.PostAsJsonAsync("api/transfer/transfer-agreements", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        return dto!.Id;
    }

    public async Task EditEndDate(Guid transferAgreementId, long endDate)
    {
        var request = new EditTransferAgreementEndDate(endDate);
        var response = await authenticatedClient.PutAsJsonAsync($"api/transfer/transfer-agreements/{transferAgreementId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public async Task<ActivityLogListEntryResponse> GetActivityLog(ActivityLogEntryFilterRequest request)
    {
        return await GetActivityLog(authenticatedClient, request);
    }

    public async Task<ActivityLogListEntryResponse> GetActivityLog(string tin, string cpn, ActivityLogEntryFilterRequest request)
    {
        return await GetActivityLog(factory.CreateAuthenticatedClient(sub, apiVersion: apiVersion, tin: tin, cpn: cpn), request);
    }

    public async Task<ActivityLogListEntryResponse> GetActivityLog(HttpClient client, ActivityLogEntryFilterRequest request)
    {
        var result = await client.PostAsJsonAsync("api/transfer/activity-log", request);
        output.WriteLine(await result.Content.ReadAsStringAsync());
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLogResponseBody = await result.Content.ReadAsStringAsync();
        var log = JsonConvert.DeserializeObject<ActivityLogListEntryResponse>(activityLogResponseBody);
        return log!;
    }

    public HttpClient MockWalletServiceAndCreateAuthenticatedClient(string receiverTin, string receiverName)
    {
        return factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: receiverTin, cpn: receiverName, apiVersion: apiVersion);
    }
}
