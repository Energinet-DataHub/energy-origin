using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Proxy.Controllers;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class ProxyTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds) => fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    private HttpClient CreateClientWithOrgAsSub(string sub) => fixture.Factory.CreateAuthenticatedClient(sub: sub);
    private HttpClient CreateUnauthenticatedClient() => fixture.Factory.CreateUnauthenticatedClient();

    [Fact]
    public async Task GivenB2C_WhenV20240515GetEndpointsAreUsed_WithInvalidOrgnaisationId_Return403Forbidden()
    {
        // Arrange
        var client = CreateClientWithOrgIds(new() { Guid.NewGuid().ToString() });

        // Act
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240515);
        var response = await client.GetAsync($"/wallet-api/wallets?organizationId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GivenB2C_WhenV20240515PostSliceEndpoint_WithoutBearerToken_Return200Ok()
    {
        // Arrange
        var endpoint = "/wallet-api/slices";
        var requestBody = new ReceiveRequest { Quantity = 1, CertificateId = new FederatedStreamId() { Registry = "test", StreamId = Guid.NewGuid() }, PublicKey = Encoding.ASCII.GetBytes("test"), RandomR = Encoding.ASCII.GetBytes("test"), Position = 1337, HashedAttributes = new List<HashedAttribute>() };
        var orgIds = new List<string> { Guid.NewGuid().ToString() };

        var requestBuilder = Request.Create()
            .WithPath($"/wallet-api/v1/slices")
            .WithBody(JsonSerializer.Serialize(requestBody))
            .UsingPost();

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("ok")
                    .WithTransformer()
            );

        var client = CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240515);

        // Act
        var response = await client.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be("ok");
    }

    [Fact]
    public async Task GivenB2C_WhenV1PostSliceEndpoint_WithoutBearerToken_Return200Ok()
    {
        // Arrange
        var endpoint = "wallet-api/v1/slices";
        var requestBody = new ReceiveRequest { Quantity = 1, CertificateId = new FederatedStreamId() { Registry = "test", StreamId = Guid.NewGuid() }, PublicKey = Encoding.ASCII.GetBytes("test"), RandomR = Encoding.ASCII.GetBytes("test"), Position = 1337, HashedAttributes = new List<HashedAttribute>() };
        var orgIds = new List<string> { Guid.NewGuid().ToString() };

        var requestBuilder = Request.Create()
            .WithPath($"/{endpoint}")
            .WithBody(JsonSerializer.Serialize(requestBody))
            .UsingPost();

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("ok")
                    .WithTransformer()
            );

        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync($"{endpoint}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be("ok");
    }

    [Fact]
    public async Task GivenB2C_WhenV20240515GetEndpointsAreUsed_WithoutEoApiVersion_Return400BadRequest()
    {
        // Arrange
        var orgId = Guid.NewGuid().ToString();
        var client = CreateClientWithOrgIds(new() { orgId });

        // Act
        var response = await client.GetAsync($"wallet-api/wallets?organizationId={orgId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/wallet-api/v1/wallets", "")]
    [InlineData("/wallet-api/v1/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/wallet-api/v1/certificates", "")]
    [InlineData("/wallet-api/v1/aggregate-certificates", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/v1/claims", "")]
    [InlineData("/wallet-api/v1/aggregate-claims", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/v1/transfers", "")]
    [InlineData("/wallet-api/v1/aggregate-transfers", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    public async Task GivenB2C_WhenV1GetEndpointsAreUsed_WithoutEoApiVersion_Return200Ok(string endpoint, string queryParameters)
    {
        // Arrange
        var requestBuilder = Request.Create().UsingGet().WithPath(endpoint);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var orgId = Guid.NewGuid().ToString();
        var client = CreateClientWithOrgAsSub(orgId);

        // Act
        var response = await client.GetAsync($"{endpoint}{queryParameters}");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgId);
    }

    [Theory]
    [InlineData("/wallet-api/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/wallet-api/wallets", "")]
    [InlineData("/wallet-api/certificates", "")]
    [InlineData("/wallet-api/aggregate-certificates", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/claims", "")]
    [InlineData("/wallet-api/aggregate-claims", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/transfers", "")]
    [InlineData("/wallet-api/aggregate-transfers", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    public async Task GivenB2C_WhenV20240515GetEndpointsAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string endpoint, string queryParameters)
    {
        // Arrange
        var orgIds = new List<string> { Guid.NewGuid().ToString() };

        var requestBuilder = Request.Create()
            .UsingGet()
            .WithPath($"/v1{endpoint}");

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var client = CreateClientWithOrgIds(orgIds);
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240515);

        // Act
        var response = await client.GetAsync($"{endpoint}?organizationId={orgIds[0]}{queryParameters}");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgIds[0]);
    }


    public class V1PostTestData : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] {"/wallet-api/v1/wallets", new CreateWalletRequest{ PrivateKey = Encoding.ASCII.GetBytes("test") }},
            new object[] {"/wallet-api/v1/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0/endpoints", new {}},
            new object[] {"/wallet-api/v1/external-endpoints", new CreateExternalEndpointRequest{TextReference = "Hello", WalletReference = new WalletEndpointReference(){ Endpoint = new Uri("https://test"), Version = 0, PublicKey = "test"}}},
            new object[] {"/wallet-api/v1/claims", new ClaimRequest{ Quantity = 1, ConsumptionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ProductionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}}},
            new object[] {"/wallet-api/v1/transfers", new TransferRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ReceiverId = Guid.NewGuid(), HashedAttributes = new []{"None :D"}}},
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(V1PostTestData))]
    public async Task GivenB2C_WhenV1PostEndpointsAreUsed_WithoutEoApiVersion_Return200Ok(string endpoint, object requestBody)
    {
        // Arrange
        var requestBuilder = Request.Create()
            .WithPath($"{endpoint}")
            .WithBody(JsonSerializer.Serialize(requestBody))
            .UsingPost();

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var orgId = Guid.NewGuid().ToString();
        var client = CreateClientWithOrgAsSub(orgId);

        // Act
        var response = await client.PostAsync($"{endpoint}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgId);
    }

    public class V20240515PostTestData : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] {"/wallets", new CreateWalletRequest{ PrivateKey = Encoding.ASCII.GetBytes("test") }},
            new object[] {"/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0/endpoints", new {}},
            new object[] {"/external-endpoints", new CreateExternalEndpointRequest{TextReference = "Hello", WalletReference = new WalletEndpointReference(){ Endpoint = new Uri("https://test"), Version = 0, PublicKey = "test"}}},
            new object[] {"/claims", new ClaimRequest{ Quantity = 1, ConsumptionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ProductionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}}},
            new object[] {"/transfers", new TransferRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ReceiverId = Guid.NewGuid(), HashedAttributes = new []{"None :D"}}},
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(V20240515PostTestData))]
    public async Task GivenB2C_WhenV20240515EndpointsPostAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string endpoint, object requestBody)
    {
        // Arrange
        var orgIds = new List<string> { Guid.NewGuid().ToString() };

        var requestBuilder = Request.Create()
            .WithPath($"/wallet-api/v1{endpoint}")
            .WithBody(JsonSerializer.Serialize(requestBody))
            .UsingPost();

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("{{request.headers.wallet-owner}}")
                    .WithTransformer()
            );

        var client = CreateClientWithOrgIds(orgIds);

        // Act
        client.DefaultRequestHeaders.Add("EO_API_VERSION", ApiVersions.Version20240515);
        var response = await client.PostAsync($"/wallet-api{endpoint}?organizationId={orgIds[0]}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgIds[0]);
    }
}
