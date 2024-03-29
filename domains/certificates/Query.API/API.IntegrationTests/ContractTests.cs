using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Attributes;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.IntegrationTests.Testcontainers;
using API.Query.API.ApiModels.Responses;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog.API;
using FluentAssertions;
using Testing.Helpers;
using Testing.Testcontainers;
using Xunit;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests;

[TestCaseOrderer(PriorityOrderer.TypeName, "API.IntegrationTests")]
public sealed class ContractTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>,
    IClassFixture<RabbitMqContainer>,
    IClassFixture<MeasurementsWireMock>,
    IClassFixture<ProjectOriginStack>
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly MeasurementsWireMock measurementsWireMock;

    public ContractTests(
        QueryApiWebApplicationFactory factory,
        PostgresContainer postgres,
        RabbitMqContainer rabbitMqContainer,
        MeasurementsWireMock measurementsWireMock,
        ProjectOriginStack projectOriginStack)
    {
        this.measurementsWireMock = measurementsWireMock;
        this.factory = factory;
        this.factory.ConnectionString = postgres.ConnectionString;
        this.factory.MeasurementsUrl = measurementsWireMock.Url;
        this.factory.RabbitMqOptions = rabbitMqContainer.Options;
        this.factory.WalletUrl = projectOriginStack.WalletUrl;
    }

    [Fact]
    public async Task CreateContract_ActivateWithEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        var expectedContract = new
        {
            GSRN = gsrn,
            StartDate = startDate,
            EndDate = endDate
        };

        createdContract.Should().BeEquivalentTo(expectedContract);
    }

    [Fact]
    public async Task CreateContract_ActivateWithEndDateAndConsumptionType_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Consumption);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        createdContract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task CreateContract_ActivateWithoutEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;

        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        createdContract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task CreateContract_GsrnAlreadyExistsInDb_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

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

        measurementsWireMock.SetupMeteringPointsResponse(gsrn1, MeteringPointType.Production);

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
    public async Task CreateContract_WithConsumptionMeteringPoint_TechnologyNull()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Consumption, technology);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;
        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        createdContract?.Technology.Should().BeNull();
    }

    [Fact]
    public async Task CreateContract_WithProductionMeteringPoint_TechnologyExists()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production, technology);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContractUri = response.Headers.Location;
        var createdContract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        var expectedTechnology = new DataContext.ValueObjects.Technology(technology.AibFuelCode, technology.AibTechCode);

        createdContract!.Technology.Should().Be(expectedTechnology);
    }

    [Fact]
    public async Task CreateContract_WhenCtreatingMultipleNonOverlappingContracts_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(2).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(3).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(4).ToUnixTimeSeconds();

        var contract1Body = new { gsrn, startDate = startDateContract1, endDate = endDateContract1 };
        var contract2Body = new { gsrn, startDate = startDateContract2, endDate = endDateContract2 };

        using var responseContract1 = await client.PostAsJsonAsync("api/certificates/contracts", contract1Body);
        using var responseContract2 = await client.PostAsJsonAsync("api/certificates/contracts", contract2Body);

        var contracts = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");

        contracts!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateContract_WhenCreatingOverlappingContracts_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(3).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(2).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(5).ToUnixTimeSeconds();

        var contract1Body = new { gsrn, startDate = startDateContract1, endDate = endDateContract1 };
        var contract2Body = new { gsrn, startDate = startDateContract2, endDate = endDateContract2 };

        using var responseContract1 = await client.PostAsJsonAsync("api/certificates/contracts", contract1Body);
        using var responseContract2 = await client.PostAsJsonAsync("api/certificates/contracts", contract2Body);

        responseContract2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var contracts = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");

        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateContract_InvalidGsrn_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var invalidGsrn = "invalid GSRN";
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

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
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        using var client = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var futureDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();

        var tenConcurrentRequests = Enumerable
            .Range(1, 10)
            .Select(_ => client.PostAsJsonAsync("api/certificates/contracts", new { gsrn, startDate = now, futureDate }));

        var responses = await Task.WhenAll(tenConcurrentRequests);

        responses.Where(r => r.StatusCode == HttpStatusCode.Created).Should().HaveCount(1);
        responses.Where(r => !r.IsSuccessStatusCode).Should().HaveCount(9);

        var contracts = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");
        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_Success()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

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
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_Ok()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var response = await client.GetFromJsonAsync<ContractList>("api/certificates/contracts");
        response!.Result.Should().BeEmpty();
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
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

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
    public async Task EditEndDate_StartsWithNoEndDate_HasEndDate()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new { endDate };

        using var editResponse = await client.PutAsJsonAsync(createdContractUri, putBody);

        var contract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task EditEndDate_SetsToNoEndDate_HasNoEndDate()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        var putBody = new { endDate = (long?)null };

        using var editResponse = await client.PutAsJsonAsync(createdContractUri, putBody);

        var contract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task EditEndDate_WithoutEndDate_Ended()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };

        using var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        var endDate = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();
        var putBody = new { endDate };

        using var editResponse = await client.PutAsJsonAsync(createdContractUri, putBody);

        var contract = await client.GetFromJsonAsync<Contract>(createdContractUri);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task EditEndDate_NoContractCreated_NoContract()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var putBody = new
        {
            endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
        };

        var nonExistingContractId = Guid.NewGuid();

        using var response = await client.PutAsJsonAsync($"api/certificates/contracts/{nonExistingContractId}", putBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_NewEndDateBeforeStartDate_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new
        {
            gsrn,
            startDate = start.ToUnixTimeSeconds(),
            endDate = start.AddYears(1).ToUnixTimeSeconds()
        };

        var response = await client.PostAsJsonAsync("api/certificates/contracts", body);

        var createdContractUri = response.Headers.Location;

        //measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);
        var putBody = new
        {
            endDate = start.AddDays(-1).ToUnixTimeSeconds()
        };

        using var editResponse = await client.PutAsJsonAsync(createdContractUri, putBody);

        editResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditEndDate_UserIsNotOwnerOfMeteringPoint_Forbidden()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new
        {
            gsrn,
            startDate = start.ToUnixTimeSeconds(),
            endDate = start.AddYears(1).ToUnixTimeSeconds()
        };

        var response = await client.PostAsJsonAsync("api/certificates/contracts", body);
        var createdContractUri = response.Headers.Location;

        client.Dispose();

        var newSubject = Guid.NewGuid().ToString();
        using var client2 = factory.CreateAuthenticatedClient(newSubject);
        var putBody = new
        {
            endDate = start.AddDays(-1).ToUnixTimeSeconds()
        };

        using var editResponse = await client2.PutAsJsonAsync(createdContractUri, putBody);

        editResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenMeteringPoint_WhenCreatingContract_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate };
        using var contractResponse = await client.PostAsJsonAsync("api/certificates/contracts", body);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert activity log entry
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Single(activityLog!.ActivityLogEntries.Where(x => x.ActorId.ToString() == subject));
    }

    [Fact]
    public async Task GivenContract_WhenEditingEndDate_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };
        using var contractResponse = await client.PostAsJsonAsync("api/certificates/contracts", body);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update end date
        var createdContractUri = contractResponse.Headers.Location;
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new { endDate };
        await client.PutAsJsonAsync(createdContractUri, putBody);

        // Assert activity log entries (created, updated)
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Equal(2, activityLog!.ActivityLogEntries.Count(x => x.ActorId.ToString() == subject));
    }

    [Fact]
    public async Task GivenContract_WhenEditingEndDate_ActivityLogIsCleanedUp()
    {
        // Create contract
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new { gsrn, startDate, endDate = (long?)null };
        using var contractResponse = await client.PostAsJsonAsync("api/certificates/contracts", body);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update end date
        var createdContractUri = contractResponse.Headers.Location;
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new { endDate };
        await client.PutAsJsonAsync(createdContractUri, putBody);

        // Wait for activity log entries to be cleaned up
        await Task.Delay(3200);

        // Assert activity log entries (created, updated)
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Empty(activityLog!.ActivityLogEntries);
    }

}
