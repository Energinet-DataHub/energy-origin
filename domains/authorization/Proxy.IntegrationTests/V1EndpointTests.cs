using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Proxy.Controllers;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class V1EndpointTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds) => fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    private HttpClient CreateClientWithOrgAsSub(string sub) => fixture.Factory.CreateAuthenticatedClient(sub: sub);


    // [Fact]
    // public async Task GivenB2C_WhenV20250101GetEndpointsAreUsed_WithInvalidOrgnaisationId_Return403Forbidden()
    // {
    //     // Arrange
    //     var client = CreateClientWithOrgIds(new (){Guid.NewGuid().ToString()});
    //
    //     // Act
    //     client.DefaultRequestHeaders.Add("EO_API_VERSION", "20250101");
    //     var response = await client.GetAsync($"wallets?organizationId={Guid.NewGuid()}");
    //
    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    // }

    [Fact]
    public async Task GivenB2C_WhenV20250101GetEndpointsAreUsed_WithoutEoApiVersion_Return400BadRequest()
    {
        // Arrange
        var orgId = Guid.NewGuid().ToString();
        var client = CreateClientWithOrgIds(new (){orgId});

        // Act
        var response = await client.GetAsync($"wallets?organizationId={orgId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/v1/wallets", "")]
    [InlineData("/v1/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/v1/certificates", "")]
    [InlineData("/v1/aggregate-certificates", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/v1/claims", "")]
    [InlineData("/v1/aggregate-claims", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/v1/transfers", "")]
    [InlineData("/v1/aggregate-transfers", "?TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
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
    [InlineData("/wallets", "")]
    [InlineData("/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/certificates", "")]
    [InlineData("/aggregate-certificates", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/claims", "")]
    [InlineData("/aggregate-claims", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/transfers", "")]
    [InlineData("/aggregate-transfers", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    public async Task GivenB2C_WhenV20250101GetEndpointsAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string endpoint, string queryParameters)
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
        client.DefaultRequestHeaders.Add("EO_API_VERSION", "20250101");

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
            new object[] {"/v1/wallets", new CreateWalletRequest{ PrivateKey = Encoding.ASCII.GetBytes("test") }},
            new object[] {"/v1/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0/endpoints", new {}},
            new object[] {"/v1/external-endpoints", new CreateExternalEndpointRequest{TextReference = "Hello", WalletReference = new WalletEndpointReference(){ Endpoint = new Uri("https://test"), Version = 0, PublicKey = "test"}}},
            new object[] {"/v1/claims", new ClaimRequest{ Quantity = 1, ConsumptionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ProductionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}}},
            // slices is our only insecure endpoint. new object[] {"/slices", new ReceiveRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, PublicKey = Encoding.ASCII.GetBytes("test"), RandomR = Encoding.ASCII.GetBytes("test"), Position = 1, HashedAttributes = new List<HashedAttribute>()}},
            new object[] {"/v1/transfers", new TransferRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ReceiverId = Guid.NewGuid(), HashedAttributes = new []{"None :D"}}},
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

    public class V20250101PostTestData : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[] {"/wallets", new CreateWalletRequest{ PrivateKey = Encoding.ASCII.GetBytes("test") }},
            new object[] {"/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0/endpoints", new {}},
            new object[] {"/external-endpoints", new CreateExternalEndpointRequest{TextReference = "Hello", WalletReference = new WalletEndpointReference(){ Endpoint = new Uri("https://test"), Version = 0, PublicKey = "test"}}},
            new object[] {"/claims", new ClaimRequest{ Quantity = 1, ConsumptionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ProductionCertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}}},
            // slices is our only insecure endpoint. new object[] {"/slices", new ReceiveRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, PublicKey = Encoding.ASCII.GetBytes("test"), RandomR = Encoding.ASCII.GetBytes("test"), Position = 1, HashedAttributes = new List<HashedAttribute>()}},
            new object[] {"/transfers", new TransferRequest{ Quantity = 1, CertificateId = new FederatedStreamId(){ Registry = "test", StreamId = Guid.NewGuid()}, ReceiverId = Guid.NewGuid(), HashedAttributes = new []{"None :D"}}},
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(V20250101PostTestData))]
    public async Task GivenB2C_WhenV20250101EndpointsPostAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string endpoint, object requestBody)
    {
        // Arrange
        var orgIds = new List<string> { Guid.NewGuid().ToString() };

        var requestBuilder = Request.Create()
            .WithPath($"/v1{endpoint}")
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
        client.DefaultRequestHeaders.Add("EO_API_VERSION", "20250101");
        var response = await client.PostAsync($"{endpoint}?organizationId={orgIds[0]}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgIds[0]);
    }
}
