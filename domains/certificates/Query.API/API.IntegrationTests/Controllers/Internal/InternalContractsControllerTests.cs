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
using API.Query.API.ApiModels.Requests.Internal;
using API.Query.API.ApiModels.Responses.Internal;
using DataContext;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Technology = API.ContractService.Clients.Technology;

namespace API.IntegrationTests.Controllers.Internal;

[Collection(IntegrationTestCollection.CollectionName)]
public class InternalContractsControllerTests(IntegrationTestFixture fixture) : TestBase(fixture)
{
    private readonly QueryApiWebApplicationFactory _factory = fixture.WebApplicationFactory;
    private readonly MeasurementsWireMock _measurementsWireMock = fixture.MeasurementsMock;

    private readonly string OrganizationTin = "11223344";
    private readonly string OrganizationName = "Peter Producent";

    [Fact]
    public async Task Given_NoContracts_When_CallingApi_Then_Return200ok_With_EmptyContractsForAdminPortalResponse()
    {
        using var clientForInternalCalls = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

        var response = await clientForInternalCalls.GetAsync("/api/certificates/contracts",
            TestContext.Current.CancellationToken);

        var str = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contractsResponse =
            await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(contractsResponse);
        Assert.Empty(contractsResponse.Result);
        Assert.IsType<ContractsForAdminPortalResponse>(contractsResponse);
    }

    [Fact]
    public async Task Given_ContractsExist_When_CallingInternalContractsEndpoints_Then_Return200ok_With_PopulatedContractsForAdminPortalResponse()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        await _factory.CreateWallet(orgId.ToString());

        using var userCreatesAContract = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var insertedIntoDb = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            },
        ], orgId, OrganizationTin, OrganizationName, false);

        await userCreatesAContract.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", insertedIntoDb,
            cancellationToken: TestContext.Current.CancellationToken);

        var adminPortalClient = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);
        using var response =
            await adminPortalClient.GetAsync("api/certificates/contracts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var internalContractsApiResponse =
            await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(internalContractsApiResponse);
        Assert.NotEmpty(internalContractsApiResponse.Result);
        Assert.Equal(internalContractsApiResponse.Result.First().GSRN, insertedIntoDb.Contracts[0].GSRN);
        Assert.IsType<ContractsForAdminPortalResponse>(internalContractsApiResponse);
    }

    [Fact]
    public async Task CreateMultipleContract_ActivateWithEndDate_Created()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var gsrn1 = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        _measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology, bool CanBeUsedForIssuingCertificates)>
            {
                (gsrn, MeteringPointType.Production, null, true),
                (gsrn1, MeteringPointType.Production, null, true)
            });

        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

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
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var contracts = await response.Content.ReadJson<ContractList>();
        contracts!.Result.Count().Should().Be(body.Contracts.Count);
    }

    [Fact]
    public async Task CreateMulitpleContract_Overlapping_Conflict()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        _measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology, bool CanBeUsedForIssuingCertificates)>
            {
                (gsrn, MeteringPointType.Production, null, true),
                (gsrn, MeteringPointType.Production, null, true)
            });

        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

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
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_ActivateWithEndDate_Created()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

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
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Consumption);

        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        createdContract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task CreateContract_ActivateWithoutEndDate_Created()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        createdContract.Should()
            .BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task CreateContract_GsrnAlreadyExistsInDb_Conflict()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var body = new CreateContracts([
            new CreateContract
            { GSRN = gsrn, StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(), EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds() }
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var conflictResponse = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_MeteringPointNotOwnedByUser_BadRequest()
    {
        var gsrn1 = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var gsrn2 = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        _measurementsWireMock.SetupMeteringPointsResponse(gsrn1, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn2,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_WithConsumptionMeteringPoint_TechnologyNull()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Consumption, technology);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        createdContract?.Technology.Should().BeNull();
    }

    [Fact]
    public async Task CreateContract_WithProductionMeteringPoint_TechnologyExists()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var technology = new Technology(AibFuelCode: "F01040100", AibTechCode: "T010000");
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production, technology);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var createdContract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        createdContract!.Technology!.AibFuelCode.Should().Be(technology.AibFuelCode);
        createdContract!.Technology!.AibTechCode.Should().Be(technology.AibTechCode);
    }

    [Fact]
    public async Task CreateContract_WhenCtreatingMultipleNonOverlappingContracts_Created()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(2).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(3).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(4).ToUnixTimeSeconds();

        var contract1Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract1, EndDate = endDateContract1 }], orgId, OrganizationTin, OrganizationName, false);
        var contract2Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract2, EndDate = endDateContract2 }], orgId, OrganizationTin, OrganizationName, false);

        using var responseContract1 = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", contract1Body, cancellationToken: TestContext.Current.CancellationToken);
        using var responseContract2 = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", contract2Body, cancellationToken: TestContext.Current.CancellationToken);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts", cancellationToken: TestContext.Current.CancellationToken);

        contracts!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateContract_WhenCreatingOverlappingContracts_Conflict()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var now = DateTimeOffset.Now;

        var startDateContract1 = now.AddDays(1).ToUnixTimeSeconds();
        var endDateContract1 = now.AddDays(3).ToUnixTimeSeconds();

        var startDateContract2 = now.AddDays(2).ToUnixTimeSeconds();
        var endDateContract2 = now.AddDays(5).ToUnixTimeSeconds();

        var contract1Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract1, EndDate = endDateContract1 }], orgId, OrganizationTin, OrganizationName, false);
        var contract2Body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDateContract2, EndDate = endDateContract2 }], orgId, OrganizationTin, OrganizationName, false);

        using var responseContract1 = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", contract1Body, cancellationToken: TestContext.Current.CancellationToken);
        using var responseContract2 = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", contract2Body, cancellationToken: TestContext.Current.CancellationToken);

        var responseContent2 = await responseContract2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent2.Should().Contain($"{contract2Body.Contracts[0].GSRN} already has an active contract");
        responseContract2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts", cancellationToken: TestContext.Current.CancellationToken);

        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateContract_InvalidGsrn_BadRequest()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var invalidGsrn = "invalid GSRN";
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = invalidGsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateContract_GsrnNotFound_BadRequest()
    {
        var gsrnNotFound = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrnNotFound,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().Contain($"GSRN {gsrnNotFound} was not found");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task CreateContract_CannotBeUsedForIssuingCertificates_Conflict()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production, canBeUsedforIssuingCertificates: false);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseContent.Should().Contain($"GSRN {gsrn} cannot be used for issuing certificates");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContract_ConcurrentRequests_OnlyOneContractCreated()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var futureDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();

        var tenConcurrentRequests = Enumerable
            .Range(1, 10)
            .Select(_ =>
                client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts",
                    new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = now, EndDate = futureDate }], orgId, OrganizationTin, OrganizationName, false)));

        var responses = await Task.WhenAll(tenConcurrentRequests);

        responses.Where(r => r.StatusCode == HttpStatusCode.Created).Should().HaveCount(1);
        responses.Where(r => !r.IsSuccessStatusCode).Should().HaveCount(9);

        var contracts = await client.GetFromJsonAsync<ContractList>($"api/certificates/contracts", cancellationToken: TestContext.Current.CancellationToken);
        contracts!.Result.Should().HaveCount(1);
    }

    [Fact]
    public async Task EditEndDate_StartsWithNoEndDate_HasEndDate()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }], orgId, OrganizationTin, OrganizationName);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task EditEndDate_SetsToNoEndDate_HasNoEndDate()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = null }], orgId, OrganizationTin, OrganizationName);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = (DateTimeOffset?)null });
    }

    [Fact]
    public async Task EditEndDate_WithoutEndDate_Ended()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);

        using var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var endDate = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }], orgId, OrganizationTin, OrganizationName);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{createdContractId}", cancellationToken: TestContext.Current.CancellationToken);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = endDate });
    }

    [Fact]
    public async Task UpdateEndDate_OverlappingContract_ReturnsConflict()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

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
        ], orgId, OrganizationTin, OrganizationName, false);
        using var createResponse = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        var response = await createResponse.Content.ReadJson<ContractList>();
        var id = response!.Result.ToList().Find(c => c.StartDate == startDate)!.Id;

        var newEndDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(8).ToUnixTimeSeconds();
        var contracts = new List<EditContractEndDate>
        {
            new() { Id = id, EndDate = newEndDate },
        };

        var updateResponse = await client.PutAsJsonAsync(
                $"api/certificates/admin-portal/internal-contracts",
                new EditContracts(contracts, orgId, OrganizationTin, OrganizationName),
                cancellationToken: TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateEndDate_MultipleContracts_ReturnsOk()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        var gsrn1 = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(
            new List<(string gsrn, MeteringPointType type, Technology? technology, bool CanBeUsedForIssuingCertificates)>
            {
                (gsrn, MeteringPointType.Production, null, true),
                (gsrn1, MeteringPointType.Production, null, true)
            });

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

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
        ], orgId, OrganizationTin, OrganizationName, false);

        using var createResponse = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
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

        var response = await client.PutAsJsonAsync(
                $"api/certificates/admin-portal/internal-contracts",
                new EditContracts(contracts, orgId, OrganizationTin, OrganizationName),
                cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var contract = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{id}", cancellationToken: TestContext.Current.CancellationToken);
        var contract1 = await client.GetFromJsonAsync<Contract>($"api/certificates/contracts/{id1}", cancellationToken: TestContext.Current.CancellationToken);

        contract.Should().BeEquivalentTo(new { GSRN = gsrn, StartDate = startDate, EndDate = newEndDate });
        contract1.Should().BeEquivalentTo(new { GSRN = gsrn1, StartDate = startDate1, EndDate = newEndDate1 });
    }

    [Fact]
    public async Task EditEndDate_NoContractCreated_NoContract()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = Guid.NewGuid(),
                EndDate = UnixTimestamp.Now().ToDateTimeOffset().AddDays(3).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName);

        var nonExistingContractId = Guid.NewGuid();

        using var response =
            await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_NewEndDateBeforeStartDate_BadRequest()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = start.ToUnixTimeSeconds(),
                EndDate = start.AddYears(1).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);

        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = createdContractId,
                EndDate = start.AddDays(-1).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName);

        using var editResponse = await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        editResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditEndDate_UserIsNotOwnerOfMeteringPoint_Forbidden()
    {
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

        var start = DateTimeOffset.Now.AddDays(3);

        var body = new CreateContracts([
            new CreateContract
            {
                GSRN = gsrn,
                StartDate = start.ToUnixTimeSeconds(),
                EndDate = start.AddYears(1).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName, false);

        var response = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        var createdContracts = await response.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;

        var newSubject = Guid.NewGuid();
        var newOrgId = Guid.NewGuid();
        using var client2 = _factory.CreateB2CAuthenticatedClient(newSubject, newOrgId, apiVersion: ApiVersions.Version1);
        var putBody = new EditContracts([
            new EditContractEndDate
            {
                Id = createdContractId,
                EndDate = start.AddDays(-1).ToUnixTimeSeconds()
            }
        ], orgId, OrganizationTin, OrganizationName);

        using var editResponse = await client2.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts?organizationId={newOrgId}", putBody, cancellationToken: TestContext.Current.CancellationToken);

        editResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenMeteringPoint_WhenCreatingContract_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = endDate }], orgId, OrganizationTin, OrganizationName, false);
        using var contractResponse = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert activity log entry
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest, cancellationToken: TestContext.Current.CancellationToken);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Single(activityLog!.ActivityLogEntries, x => x.ActorId.ToString() == subject.ToString());
    }

    [Fact]
    public async Task GivenContract_WhenEditingEndDate_ActivityLogIsUpdated()
    {
        // Create contract
        var gsrn = EnergyTrackAndTrace.Testing.Any.Gsrn().Value;
        _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await _factory.CreateWallet(orgId.ToString());

        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId);
        var startDate = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateContracts([new CreateContract { GSRN = gsrn, StartDate = startDate, EndDate = null }], orgId, OrganizationTin, OrganizationName, false);
        using var contractResponse = await client.PostAsJsonAsync($"api/certificates/admin-portal/internal-contracts", body, cancellationToken: TestContext.Current.CancellationToken);
        contractResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Update end date
        var createdContracts = await contractResponse.Content.ReadJson<ContractList>();
        var createdContractId = createdContracts!.Result.First().Id;
        var endDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds();
        var putBody = new EditContracts([new EditContractEndDate { Id = createdContractId, EndDate = endDate }], orgId, OrganizationTin, OrganizationName);
        await client.PutAsJsonAsync($"api/certificates/admin-portal/internal-contracts", putBody, cancellationToken: TestContext.Current.CancellationToken);

        // Assert activity log entries (created, updated)
        var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
        using var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest, cancellationToken: TestContext.Current.CancellationToken);
        activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
        Assert.Equal(2, activityLog!.ActivityLogEntries.Count(x => x.ActorId.ToString() == subject.ToString()));
    }

    [Fact]
    public async Task GivenOldActivityLog_WhenCleaningUp_ActivityLogIsRemoved()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        var activityLogEntry = CreateActivityLogEligiblyForCleanup();
        dbContext.ActivityLogs.Add(activityLogEntry);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var subject = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        using var client = _factory.CreateB2CAuthenticatedClient(subject, orgId);

        // Wait for activity log entries to be cleaned up
        await WaitForCondition(TimeSpan.FromSeconds(10), async ctx =>
        {
            var activityLogRequest = new ActivityLogEntryFilterRequest(null, null, null);
            var activityLogResponse = await client.PostAsJsonAsync("api/certificates/activity-log", activityLogRequest, cancellationToken: ctx);
            activityLogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var activityLog = await activityLogResponse.Content.ReadJson<ActivityLogListEntryResponse>();
            return !activityLog!.ActivityLogEntries.Where(al => al.ActorId == activityLogEntry.ActorId).Any();
        }, CancellationToken.None);
    }

    private static ActivityLogEntry CreateActivityLogEligiblyForCleanup()
    {
        var activityLogEntry = ActivityLogEntry.Create(Guid.NewGuid(), ActivityLogEntry.ActorTypeEnum.System, "", "", "", "", "",
            ActivityLogEntry.EntityTypeEnum.MeteringPoint, ActivityLogEntry.ActionTypeEnum.Activated, "");
        typeof(ActivityLogEntry).GetProperty(nameof(ActivityLogEntry.Timestamp))!.SetValue(activityLogEntry, DateTimeOffset.UtcNow.AddDays(-60));
        return activityLogEntry;
    }

    private static async Task WaitForCondition(TimeSpan timeout, Func<CancellationToken, Task<bool>> conditionAction, CancellationToken cancellationToken)
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
