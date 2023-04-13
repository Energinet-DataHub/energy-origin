using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.AppTests.Helpers;
using API.AppTests.Infrastructure.Attributes;
using API.AppTests.Infrastructure.Factories;
using API.AppTests.Infrastructure.Testcontainers;
using API.AppTests.Mocks;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using Xunit;

namespace API.AppTests;

[WriteToConsole]
[TestCaseOrderer(PriorityOrderer.TypeName, "API.AppTests")]
public sealed class ContractTests :
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<MartenDbContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<DataSyncWireMock>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly DataSyncWireMock dataSyncWireMock;

    public ContractTests(
        QueryApiWebApplicationFactory factory,
        MartenDbContainer marten,
        RabbitMqContainer rabbitMqContainer,
        DataSyncWireMock dataSyncWireMock)
    {
        this.dataSyncWireMock = dataSyncWireMock;
        this.factory = factory;
        this.factory.MartenConnectionString = marten.ConnectionString;
        this.factory.DataSyncUrl = dataSyncWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
    }

    [Fact]
    public async Task CreateContract_Activate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        using var createdContractResponse = await client.GetAsync(createdContractUri);

        createdContractResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateContract_GsrnAlreadyExistsInDb_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var response2 = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_MeteringPointNotOwnedByUser_BadRequest()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();

        dataSyncWireMock.SetupMeteringPointsResponse(gsrn1);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = gsrn2, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_MeteringPointIsConsumption_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn, type: "consumption");

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_InvalidGsrn_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var invalidGsrn = "invalid GSRN";
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn = invalidGsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestPriority(1)]
    [Fact]
    public async Task CreateContract_ConcurrentRequests_OnlyOneContractCreated()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();

        var tenConcurrentRequests = Enumerable
            .Range(1, 10)
            .Select(_ => client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now }));

        var responses = await Task.WhenAll(tenConcurrentRequests);

        responses.Where(r => r.StatusCode == HttpStatusCode.Created).Should().HaveCount(1);
        responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).Should().HaveCount(9);

        var contracts = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");
        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_Success()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new { gsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };
        await client.PostAsJsonAsync("api/certificates/contracts", body);

        using var response = await client.GetAsync("api/certificates/contracts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_NoContent()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        using var response = await client.GetAsync("api/certificates/contracts");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetSpecificContract_ContractDoesNotExist_NotFound()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var contractId = Guid.NewGuid().ToString();
        using var response = await client.GetAsync($"api/certificates/contracts/{contractId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpecificContract_UserIsNotOwner_NotFound()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject1 = Guid.NewGuid().ToString();
        using var client1 = factory.CreateAuthenticatedClient(subject1);

        var body = new { gsrn, startDate = DateTimeOffset.Now.ToUnixTimeSeconds() };

        using var response = await client1.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var subject2 = Guid.NewGuid().ToString();
        using var client2 = factory.CreateAuthenticatedClient(subject2);

        using var getSpecificContractResponse = await client2.GetAsync(createdContractUri);
        getSpecificContractResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
