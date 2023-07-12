using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Attributes;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Helpers;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

[TestCaseOrderer(PriorityOrderer.TypeName, "API.IntegrationTests")]
public sealed class ContractTests :
    TestBase,
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
    public async Task CreateContract_ActivateWithEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new
        {
            gsrn,
            startDate = startDate,
            endDate = endDate
        };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        createdContract.Should().NotBeNull();
        createdContract.GSRN.Should().Be(gsrn);
        createdContract.StartDate.Should().Be(startDate);
        createdContract.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task CreateContract_ActivateWithoutEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new
        {
            gsrn,
            startDate = startDate,
            endDate = (long?)null
        };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        createdContract.Should().NotBeNull();
        createdContract.GSRN.Should().Be(gsrn);
        createdContract.StartDate.Should().Be(startDate);
        createdContract.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateContract_GsrnAlreadyExistsInDb_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var conflictResponse = await client.PostAsJsonAsync("api/certificates/contracts", body);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_MeteringPointNotOwnedByUser_BadRequest()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();

        dataSyncWireMock.SetupMeteringPointsResponse(gsrn1);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new
        {
            gsrn = gsrn2,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

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

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

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

        var body = new
        {
            invalidGsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

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
        var futureDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();

        var tenConcurrentRequests = Enumerable
            .Range(1, 10)
            .Select(_ => client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now, futureDate }));

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

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

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

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

        using var response = await client1.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var subject2 = Guid.NewGuid().ToString();
        using var client2 = factory.CreateAuthenticatedClient(subject2);

        using var getSpecificContractResponse = await client2.GetAsync(createdContractUri);
        getSpecificContractResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndContract_End_Ended()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        var endContractBody = new EndContract
        {
            EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

        using var endContractResponse = await client.PatchAsJsonAsync(createdContractUri, endContractBody);

        endContractResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndContract_WithoutEndDate_Ended()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        var endContractBody = new EndContract
        {
            EndDate = null
        };

        using var endContractResponse = await client.PatchAsJsonAsync(createdContractUri, endContractBody);

        endContractResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        //TODO: Actually assert that there is a change
    }

    [Fact]
    public async Task EndContract_NoContractCreated_NoContract()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var endContractBody = new EndContract
        {
            EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

        var nonExistingContractId = Guid.NewGuid();

        using var response = await client.PatchAsJsonAsync($"api/certificates/contracts/{nonExistingContractId}", endContractBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndContract_TimeBefore_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var body = new
        {
            gsrn,
            startDate = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        dataSyncWireMock.SetupMeteringPointsResponse(gsrn);
        var endContractBody = new
        {
            gsrn,
            endDate = DateTimeOffset.Now.AddDays(-3).ToUnixTimeSeconds()
        };

        using var endContractResponse = await client.PatchAsJsonAsync(createdContractUri, endContractBody);

        endContractResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
