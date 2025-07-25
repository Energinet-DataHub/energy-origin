using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using DataContext.ValueObjects;
using EnergyOrigin.Setup;
using Xunit;

namespace API.IntegrationTests.Controllers.Internal;

[Collection(IntegrationTestCollection.CollectionName)]
public class InternalContractsControllerTests(IntegrationTestFixture fixture) : TestBase(fixture)
{
    private readonly QueryApiWebApplicationFactory _factory = fixture.WebApplicationFactory;
    private readonly MeasurementsWireMock _measurementsWireMock = fixture.MeasurementsMock;

    [Fact]
    public async Task Given_NoContracts_When_CallingApi_Then_Return200ok_With_EmptyContractsForAdminPortalResponse()
    {
        using var clientForInternalCalls = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);

        var response = await clientForInternalCalls.GetAsync("/api/certificates/admin-portal/internal-contracts",
            TestContext.Current.CancellationToken);

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
            }
        ]);

        await userCreatesAContract.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", insertedIntoDb,
            cancellationToken: TestContext.Current.CancellationToken);

        var adminPortalClient = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalEnterpriseAppRegistrationObjectId, Guid.Empty);
        using var response =
            await adminPortalClient.GetAsync("api/certificates/admin-portal/internal-contracts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var internalContractsApiResponse =
            await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(internalContractsApiResponse);
        Assert.NotEmpty(internalContractsApiResponse.Result);
        Assert.Equal(internalContractsApiResponse.Result.First().GSRN, insertedIntoDb.Contracts[0].GSRN);
        Assert.IsType<ContractsForAdminPortalResponse>(internalContractsApiResponse);
    }
}
