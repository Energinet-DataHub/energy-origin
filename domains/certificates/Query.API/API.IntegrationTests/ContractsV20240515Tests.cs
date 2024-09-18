using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Extensions;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using API.Query.API.Controllers;
using DataContext;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Testing.Helpers;
using Xunit;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class ContractsV20240515Tests
{
    private readonly QueryApiWebApplicationFactory factory;
    private readonly MeasurementsWireMock measurementsWireMock;

    public ContractsV20240515Tests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
        measurementsWireMock = integrationTestFixture.MeasurementsMock;
    }

    [Fact]
    public async Task GivenTokenValidationToken_WhenUsingNewEndpoint_RequestIsUnauthenticated()
    {
        var subject = Guid.NewGuid().ToString();
        using var client = factory.CreateAuthenticatedClient(subject, apiVersion: ApiVersions.Version20240515);
        using var response = await client.PostAsJsonAsync($"api/certificates/contracts", new CreateContracts(new List<CreateContract>()));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenB2CToken_WhenUsingOldEndpoint_RequestIsUnauthenticated()
    {
        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240423);
        using var response = await client.PostAsJsonAsync($"api/certificates/contracts", new CreateContracts(new List<CreateContract>()));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMultipleContract_ActivateWithEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var gsrn1 = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology)>
            {
                (gsrn, MeteringPointType.Production, null),
                (gsrn1, MeteringPointType.Production, null)
            });

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                EndDate = endDate,
                StartDate = startDate
            },

            new CreateContract
            {
                GSRN = gsrn1,
                EndDate = endDate,
                StartDate = startDate
            }
        ]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var contracts = await response.Content.ReadJson<ContractList>();
        contracts!.Result.Count().Should().Be(body.Contracts.Count);
    }

    [Fact]
    public async Task CreateMulitpleContract_Overlapping_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology)>
            {
                (gsrn, MeteringPointType.Production, null),
                (gsrn, MeteringPointType.Production, null)
            });

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var overlappingEnddate = DateTimeOffset.Now.AddDays(2).ToUnixTimeSeconds();
        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                EndDate = endDate,
                StartDate = startDate
            },

            new CreateContract
            {
                GSRN = gsrn,
                EndDate = overlappingEnddate,
                StartDate = startDate
            }
        ]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_ActivateWithEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

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

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        createdContract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task CreateContract_ActivateWithoutEndDate_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        createdContract.Should()
            .BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task CreateContract_GsrnAlreadyExistsInDb_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var body = new CreateContracts([
            new CreateContract
            { GSRN = gsrn, StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(), EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds() }
        ]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var conflictResponse = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_MeteringPointNotOwnedByUser_BadRequest()
    {
        var gsrn1 = GsrnHelper.GenerateRandom();
        var gsrn2 = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(gsrn1, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn2,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_WithConsumptionMeteringPoint_TechnologyNull()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Consumption, technology);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        createdContract?.Technology.Should().BeNull();
    }

    [Fact]
    public async Task CreateContract_WithProductionMeteringPoint_TechnologyExists()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production, technology);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");
        
        if (createdContract?.Technology == null)
        {
            Assert.Fail("Technology is null. Expected Technology to be not null.");
        }
        else
        {
            createdContract!.Technology.AibFuelCode.Should().Be(technology.AibFuelCode);
            createdContract!.Technology.AibTechCode.Should().Be(technology.AibTechCode);
        }
    }

    [Fact]
    public async Task CreateContract_WhenCtreatingMultipleNonOverlappingContracts_Created()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(2).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(3).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(4).ToUnixTimeSeconds();

        var contract1Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract1, EndDate = endDateContract1 }]);
        var contract2Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract2, EndDate = endDateContract2 }]);

        using var responseContract1 = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", contract1Body);
        using var responseContract2 = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", contract2Body);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts?organizationId={orgId}");

        contracts!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateContract_WhenCreatingOverlappingContracts_Conflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();

        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(3).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(2).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(5).ToUnixTimeSeconds();

        var contract1Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract1, EndDate = endDateContract1 }]);
        var contract2Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract2, EndDate = endDateContract2 }]);

        using var responseContract1 = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", contract1Body);
        using var responseContract2 = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", contract2Body);

        responseContract2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts?organizationId={orgId}");

        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateContract_InvalidGsrn_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var invalidGsrn = "invalid GSRN";
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = invalidGsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_ConcurrentRequests_OnlyOneContractCreated()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var futureDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();

        var tenConcurrentRequests = Enumerable
            .Range(1, 10)
            .Select(_ =>
                client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}",
                    new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = now, EndDate = futureDate }])));

        var responses = await Task.WhenAll(tenConcurrentRequests);

        responses.Where(r => r.StatusCode == HttpStatusCode.Created).Should().HaveCount(1);
        responses.Where(r => !r.IsSuccessStatusCode).Should().HaveCount(9);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts?organizationId={orgId}");
        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_Success()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ]);

        await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        using var response = await client.GetAsync($"api/certificates/contracts?organizationId={orgId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllMeteringPointOwnerContract_QueryAllContracts_Ok()
    {
        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var response = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts?organizationId={orgId}");
        response!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSpecificContract_ContractDoesNotExist_NotFound()
    {
        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var contractId = Guid.NewGuid().ToString();
        using var response = await client.GetAsync($"api/certificates/contracts/{contractId}?organizationId={orgId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSpecificContract_UserIsNotOwner_NotFound()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject1 = Guid.NewGuid();
        var orgId1 = Guid.NewGuid();
        using var client1 = factory.CreateB2CAuthenticatedClient(subject1, orgId1, apiVersion: ApiVersions.Version20240515);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ]);

        using var response = await client1.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId1}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var subject2 = Guid.NewGuid();
        var orgId2 = Guid.NewGuid();
        using var client2 = factory.CreateB2CAuthenticatedClient(subject2, orgId2, apiVersion: ApiVersions.Version20240515);

        using var getSpecificContractResponse = await client2.GetAsync($"api/certificates/contracts/{createdContractId}?organizationId={orgId2}");
        getSpecificContractResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_StartsWithNoEndDate_HasEndDate()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }]);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task EditEndDate_SetsToNoEndDate_HasNoEndDate()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = (long?)null }]);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task EditEndDate_WithoutEndDate_Ended()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);

        using var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var endDate = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }]);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}?organizationId={orgId}");

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task UpdateEndDate_OverlappingContract_ReturnsConflict()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(1).ToUnixTimeSeconds();
        var endDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(3).ToUnixTimeSeconds();
        var startDate1 = UnixTimestamp.Now().ToDateTimeOffset().AddDays(7).ToUnixTimeSeconds();
        var endDate1 = UnixTimestamp.Now().ToDateTimeOffset().AddDays(9).ToUnixTimeSeconds();
        var body = new CreateContracts([
            new CreateContract
            {
                EndDate = endDate1,
                GSRN = gsrn,
                StartDate = startDate1
            },
            new CreateContract
            {
                EndDate = endDate,
                GSRN = gsrn,
                StartDate = startDate
            }
        ]);
        using var createResponse = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        var response = await createResponse.Content.ReadJson<ContractList>();
        var id = response!.Result.ToList().Find(c => c.StartDate == startDate)!.Id;

        var newEndDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(8).ToUnixTimeSeconds();
        var contracts = new List<EditContractEndDate>
        {
            new() { Id = id, EndDate = newEndDate },
        };

        var updateResponse = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", new EditContracts(contracts));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateEndDate_MultipleContracts_ReturnsOk()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        var gsrn1 = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology)>
            {
                (gsrn, MeteringPointType.Production, null),
                (gsrn1, MeteringPointType.Production, null)
            });

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var startDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(1).ToUnixTimeSeconds();
        var startDate1 = UnixTimestamp.Now().ToDateTimeOffset().AddDays(2).ToUnixTimeSeconds();

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn1,
                StartDate = startDate1
            },

            new CreateContract
            {
                GSRN = gsrn,
                StartDate = startDate
            }
        ]);
        using var createResponse = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        var createContent = await createResponse.Content.ReadJson<ContractList>();

        var id = createContent!.Result.ToList().Find(c => c.StartDate == startDate)!.Id;
        var id1 = createContent!.Result.ToList().Find(c => c.StartDate == startDate1)!.Id;

        var newEndDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(4).ToUnixTimeSeconds();
        var newEndDate1 = UnixTimestamp.Now().ToDateTimeOffset().AddDays(9).ToUnixTimeSeconds();
        var contracts = new List<EditContractEndDate>
        {
            new() { Id = id, EndDate = newEndDate },
            new() { Id = id1, EndDate = newEndDate1 }
        };

        var response = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", new EditContracts(contracts));
        response.EnsureSuccessStatusCode();

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{id}?organizationId={orgId}");
        var contract1 = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{id1}?organizationId={orgId}");

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = newEndDate });
        contract1.Should().BeEquivalentTo(new { GSRN = gsrn1, StartDate = startDate1, EndDate = newEndDate1 });
    }

    [Fact]
    public async Task EditEndDate_NoContractCreated_NoContract()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = Guid.NewGuid(),
                EndDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(3).ToUnixTimeSeconds()
            }
        ]);

        var nonExistingContractId = Guid.NewGuid();

        using var response =
            await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_NewEndDateBeforeStartDate_BadRequest()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = start.ToUnixTimeSeconds(),
                EndDate = start.AddYears(1).ToUnixTimeSeconds()
            }
        ]);

        var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = createdContractId,
                EndDate = start.AddDays(-1).ToUnixTimeSeconds()
            }
        ]);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        editResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditEndDate_UserIsNotOwnerOfMeteringPoint_Forbidden()
    {
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version20240515);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = start.ToUnixTimeSeconds(),
                EndDate = start.AddYears(1).ToUnixTimeSeconds()
            }
        ]);

        var response = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var newSubject = Guid.NewGuid();
        var newOrgId = Guid.NewGuid();
        using var client2 = factory.CreateB2CAuthenticatedClient(newSubject, newOrgId, apiVersion: ApiVersions.Version20240515);
        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = createdContractId,
                EndDate = start.AddDays(-1).ToUnixTimeSeconds()
            }
        ]);

        using var editResponse = await client2.PutAsJsonAsync($"api/certificates/contracts?organizationId={newOrgId}", putBody);

        editResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenMeteringPoint_WhenCreatingContract_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }]);
        using var contractResponse = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert activity log entry
        using var oldTokenClient = factory.CreateAuthenticatedClient(orgId.ToString());
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await oldTokenClient.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Single(activityLog!.ActivityLogEntries, x => x.ActorId.ToString() == subject.ToString());
    }

    [Fact]
    public async Task GivenContract_WhenEditingEndDate_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = GsrnHelper.GenerateRandom();
        measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = factory.CreateB2CAuthenticatedClient(subject, orgId);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = (long?)null }]);
        using var contractResponse = await client.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", body);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update end date
        var createdContracts = await contractResponse.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }]);
        await client.PutAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", putBody);

        // Assert activity log entries (created, updated)
        using var oldTokenClient = factory.CreateAuthenticatedClient(orgId.ToString());
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await oldTokenClient.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Equal(2, activityLog!.ActivityLogEntries.Count(x => x.ActorId.ToString() == subject.ToString()));
    }

    [Fact]
    public async Task GivenOldActivityLog_WhenCleaningUp_ActivityLogIsRemoved()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        var activityLogEntry = CreateActivityLogEligiblyForCleanup();
        dbContext.ActivityLogs.Add(activityLogEntry);
        await dbContext.SaveChangesAsync();

        var orgId = Guid.NewGuid();
        using var oldTokenClient = factory.CreateAuthenticatedClient(orgId.ToString());

        // Wait for activity log entries to be cleaned up
        await WaitForCondition(TimeSpan.FromSeconds(10), async ctx =>
        {
            var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
            var activityLogResponse = await oldTokenClient.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest);
            activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
            return activityLog!.ActivityLogEntries.Where(al => al.ActorId == activityLogEntry.ActorId).Count() == 0;
        });
    }

    private static ActivityLogEntry CreateActivityLogEligiblyForCleanup()
    {
        var activityLogEntry = ActivityLogEntry.Create(Guid.NewGuid(), ActivityLogEntry.ActorTypeEnum.System, "", "", "", "", "",
            ActivityLogEntry.EntityTypeEnum.MeteringPoint, ActivityLogEntry.ActionTypeEnum.Activated, "");
        typeof(ActivityLogEntry).GetProperty(nameof(ActivityLogEntry.Timestamp))!.SetValue(activityLogEntry, DateTimeOffset.UtcNow.AddDays(-60));
        return activityLogEntry;
    }

    private async Task WaitForCondition(TimeSpan timeout, Func<CancellationToken, Task<bool>> conditionAction)
    {
        await WaitForCondition(timeout, CancellationToken.None, conditionAction);
    }

    private async Task WaitForCondition(TimeSpan timeout, CancellationToken cancellationToken, Func<CancellationToken, Task<bool>> conditionAction)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tokenSource.CancelAfter(timeout);
        var conditionFulfilled = false;

        while (!tokenSource.IsCancellationRequested)
        {
            conditionFulfilled = await conditionAction.Invoke(tokenSource.Token);
            if (conditionFulfilled)
            {
                return;
            }
        }

        Assert.True(conditionFulfilled, $"Condition was not fulfilled withing timeout {timeout}");
    }
}
