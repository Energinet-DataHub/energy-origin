using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Mocks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Controllers.Internal;
using DataContext.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.WalletClient;
using Testing.Helpers;
using Xunit;

namespace API.IntegrationTests.Controllers.Internal
{
    public class InternalContractsControllerTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly QueryApiWebApplicationFactory _factory;
        private readonly MeasurementsWireMock _measurementsWireMock;

        public InternalContractsControllerTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _factory = fixture.WebApplicationFactory;
            _measurementsWireMock = fixture.MeasurementsMock;
        }

        [Fact]
        public async Task Given_NoContracts_When_CallingApi_Then_Return200ok_With_EmptyContractsForAdminPortalResponse()
        {
            // Arrange
            using var clientForInternalCalls = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalClientId, Guid.Empty);

            // Act
            var response = await clientForInternalCalls.GetAsync("/api/certificates/admin-portal/internal-contracts");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contractsResponse = await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>();
            Assert.NotNull(contractsResponse);
            Assert.Empty(contractsResponse.Result);
            Assert.IsType<ContractsForAdminPortalResponse>(contractsResponse);
        }

        [Fact]
        public async Task Given_ContractsExist_When_CallingInternalContractsEndpoints_Then_Return200ok_With_PopulatedContractsForAdminPortalResponse()
        {
            // Arrange
            var gsrn = GsrnHelper.GenerateRandom();
            _measurementsWireMock.SetupMeteringPointsResponse(gsrn, MeteringPointType.Production);

            var subject = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            var walletHttpClient = new HttpClient();
            walletHttpClient.BaseAddress = new Uri(_fixture.WalletUrl);
            var walletClient = new WalletClient(walletHttpClient);
            await walletClient.CreateWallet(orgId, CancellationToken.None);

            using var userCreatesAContract = _factory.CreateB2CAuthenticatedClient(subject, orgId, apiVersion: ApiVersions.Version1);

            var insertedIntoDb = new CreateContracts([
                new CreateContract
                {
                    GSRN = gsrn,
                    StartDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    EndDate = DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds()
                }
            ]);

            await userCreatesAContract.PostAsJsonAsync($"api/certificates/contracts?organizationId={orgId}", insertedIntoDb);

            // Act
            var adminPortalClient = _factory.CreateB2CAuthenticatedClient(_factory.AdminPortalClientId, Guid.Empty);
            using var response = await adminPortalClient.GetAsync("api/certificates/admin-portal/internal-contracts");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var internalContractsApiResponse = await response.Content.ReadFromJsonAsync<ContractsForAdminPortalResponse>();
            Assert.NotNull(internalContractsApiResponse);
            Assert.NotEmpty(internalContractsApiResponse.Result);
            Assert.Equal(internalContractsApiResponse.Result.First().GSRN, insertedIntoDb.Contracts[0].GSRN);
            Assert.IsType<ContractsForAdminPortalResponse>(internalContractsApiResponse);
        }
    }
}
