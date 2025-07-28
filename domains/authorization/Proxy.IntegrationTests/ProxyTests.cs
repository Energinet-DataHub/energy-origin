using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EnergyOrigin.Setup;
using FluentAssertions;
using Proxy.Controllers;
using Proxy.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Proxy.IntegrationTests;

public class ProxyTests(ProxyIntegrationTestFixture fixture) : IClassFixture<ProxyIntegrationTestFixture>
{
    private HttpClient CreateClientWithOrgIds(List<string> orgIds) => fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds);
    private HttpClient CreateUnauthenticatedClient() => fixture.Factory.CreateUnauthenticatedClient();

    [Fact]
    public async Task GivenB2C_WhenV20240515GetEndpointsAreUsed_WithInvalidOrgnaisationId_Return403Forbidden()
    {
        // Arrange
        var client = CreateClientWithOrgIds(new() { Guid.NewGuid().ToString() });

        // Act
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
        var response = await client.GetAsync($"/wallet-api/wallets?organizationId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

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
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);

        // Act
        var response = await client.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

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
        var response = await client.PostAsync($"{endpoint}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

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
        var response = await client.GetAsync($"wallet-api/wallets?organizationId={orgId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/wallet-api/wallets/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/wallet-api/wallets", "")]
    [InlineData("/wallet-api/certificates", "")]
    [InlineData("/wallet-api/certificates/energy-origin/8229a340-1c9d-46b6-8212-b767e42e02f0", "")]
    [InlineData("/wallet-api/aggregate-certificates", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/claims", "")]
    [InlineData("/wallet-api/aggregate-claims", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/transfers", "")]
    [InlineData("/wallet-api/aggregate-transfers", "&TimeAggregate=hour&TimeZone=UTC&Start=1622505600&End=1625097600")]
    [InlineData("/wallet-api/request-status/f07743a2-744b-4df4-9320-c132f36333dc", "")]
    public async Task GivenB2C_WhenV20240515GetEndpointsAreUsed_ThenAppendQueryParameterAsWalletOwnerHeader(string endpoint, string queryParameters)
    {
        // Arrange
        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var mockEndpoint = $"/wallet-api/v1{endpoint.Remove(0, 11)}";

        var requestBuilder = Request.Create()
            .UsingGet()
            .WithPath(mockEndpoint);

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
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);

        // Act
        var response = await client.GetAsync($"{endpoint}?organizationId={orgIds[0]}{queryParameters}", TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgIds[0]);
    }

    [Theory]
    [InlineData("trial", "Trial")]
    [InlineData("", "NonTrial")]
    [InlineData("active", "NonTrial")]
    public async Task GivenB2C_WhenAggregateClaimsEndpointIsCalled_ThenTrialFilterParameterIsSetCorrectly(string orgStatus, string expectedTrialFilter)
    {
        var orgIds = new List<string> { Guid.NewGuid().ToString() };
        var mockEndpoint = "/wallet-api/v1/aggregate-claims";

        var requestBuilder = Request.Create()
            .UsingGet()
            .WithPath(mockEndpoint)
            .WithParam("TrialFilter", expectedTrialFilter);

        fixture.WalletWireMockServer
            .Given(requestBuilder)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{\"result\":[],\"metadata\":{\"count\":0,\"offset\":0,\"limit\":10,\"total\":0}}")
            );

        var client = fixture.Factory.CreateAuthenticatedClient(orgIds: orgIds, orgStatus: orgStatus);
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);

        var response = await client.GetAsync($"/wallet-api/aggregate-claims?organizationId={orgIds[0]}&TimeAggregate=hour&TimeZone=UTC", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public class V20240515PostTestData : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
        {
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
        client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
        var response = await client.PostAsync($"/wallet-api{endpoint}?organizationId={orgIds[0]}", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().Be(orgIds[0]);
    }
}
