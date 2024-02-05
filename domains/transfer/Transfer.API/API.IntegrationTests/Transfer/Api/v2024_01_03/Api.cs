using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Services;
using API.Transfer.Api.v2024_01_03.Dto.Requests;
using API.Transfer.Api.v2024_01_03.Dto.Responses;
using EnergyOrigin.ActivityLog.API;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit.Abstractions;

namespace API.IntegrationTests.Transfer.Api.v2024_01_03;

public class Api
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly ITestOutputHelper output;
    private readonly string sub;
    private readonly HttpClient authenticatedClient;
    private readonly string apiVersion = "20240103";

    public Api(TransferAgreementsApiWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
        sub = Guid.NewGuid().ToString();
        authenticatedClient = factory.CreateAuthenticatedClient(sub, apiVersion: apiVersion);
    }

    public async Task<Guid> CreateTransferAgreementProposal(CreateTransferAgreementProposal request)
    {
        var result = await authenticatedClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        output.WriteLine(await result.Content.ReadAsStringAsync());
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        return createdProposal!.Id;
    }

    public async Task AcceptTransferAgreementProposal(string receiverTin, Guid createdProposalId)
    {
        var receiverClient = MockWalletServiceAndCreateAuthenticatedClient(receiverTin);
        var transferAgreement = new CreateTransferAgreement(createdProposalId);

        var response = await receiverClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public async Task<ActivityLogListEntryResponse> GetActivityLog(ActivityLogEntryFilterRequest request)
    {
        return await GetActivityLog(authenticatedClient, request);
    }

    public async Task<ActivityLogListEntryResponse> GetActivityLog(string tin, ActivityLogEntryFilterRequest request)
    {
        return await GetActivityLog(factory.CreateAuthenticatedClient(sub, apiVersion: apiVersion, tin: tin), request);
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

    public HttpClient MockWalletServiceAndCreateAuthenticatedClient(string receiverTin)
    {
        return factory.CreateAuthenticatedClient(SetupPoWalletServiceMock(), sub: Guid.NewGuid().ToString(), tin: receiverTin, apiVersion: apiVersion);
    }

    private IProjectOriginWalletService SetupPoWalletServiceMock()
    {
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        poWalletServiceMock.CreateWalletDepositEndpoint(Arg.Any<AuthenticationHeaderValue>()).Returns("SomeToken");
        poWalletServiceMock.CreateReceiverDepositEndpoint(Arg.Any<AuthenticationHeaderValue>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Guid.NewGuid());
        return poWalletServiceMock;
    }
}
