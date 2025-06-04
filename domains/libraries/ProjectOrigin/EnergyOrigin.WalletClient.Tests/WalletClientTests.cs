using EnergyOrigin.WalletClient.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using System.Net;
using Xunit;

namespace EnergyOrigin.WalletClient.Tests;

public class WalletClientTests(ProjectOriginStack poStack) : IClassFixture<ProjectOriginStack>
{
    [Fact]
    public async Task CreateAndGetWallets()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        createWalletResponse.Should().NotBeNull();

        var wallets = await walletClient.GetWalletsAsync(ownerSubject, CancellationToken.None);

        wallets.Should().NotBeNull();
        wallets.Result.Count().Should().Be(1);
    }

    [Fact]
    public async Task DisableWallet()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        Assert.NotNull(createWalletResponse);

        var disableWalletResponse = await walletClient.DisableWalletAsync(createWalletResponse.WalletId, ownerSubject, CancellationToken.None);

        Assert.NotNull(disableWalletResponse);
        Assert.Equal(createWalletResponse.WalletId, disableWalletResponse.WalletId);
    }

    [Fact]
    public async Task GetWallets_WhenNoWallets_ExpectEmptyResult()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var wallets = await walletClient.GetWalletsAsync(ownerSubject, CancellationToken.None);

        wallets.Should().NotBeNull();
        wallets.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWalletEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpointAsync(createWalletResponse.WalletId, ownerSubject, CancellationToken.None);

        walletEndpoint.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateExternalEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpointAsync(createWalletResponse.WalletId, ownerSubject, CancellationToken.None);

        walletEndpoint.Should().NotBeNull();
        var cvrNumber = "12345678";
        var externalEndpoint = await walletClient.CreateExternalEndpointAsync(Guid.NewGuid(), walletEndpoint, cvrNumber, CancellationToken.None);

        externalEndpoint.Should().NotBeNull();
        externalEndpoint.ReceiverId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TransferCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);

        var createWalletResponse = await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpointAsync(createWalletResponse.WalletId, ownerSubject, CancellationToken.None);

        walletEndpoint.Should().NotBeNull();
        var cvrNumber = "12345678";
        var externalEndpoint = await walletClient.CreateExternalEndpointAsync(Guid.NewGuid(), walletEndpoint, cvrNumber, CancellationToken.None);

        externalEndpoint.Should().NotBeNull();
        externalEndpoint.ReceiverId.Should().NotBeEmpty();

        //This does not go well in the wallet since we haven't sent the certificate to the registry first,
        //but for this test we don't care
        var cert = new GranularCertificate
        {
            CertificateType = CertificateType.Production,
            Attributes = new Dictionary<string, string>(),
            End = new DateTimeOffset().AddHours(1).ToUnixTimeSeconds(),
            FederatedStreamId = new FederatedStreamId { Registry = "DK1", StreamId = Guid.NewGuid() },
            GridArea = "DK1",
            Quantity = 10,
            Start = new DateTimeOffset().ToUnixTimeSeconds()
        };
        var transferCertificatesResponse = await walletClient.TransferCertificatesAsync(ownerSubject, cert, cert.Quantity, externalEndpoint.ReceiverId, CancellationToken.None);

        transferCertificatesResponse.Should().NotBeNull();
        transferCertificatesResponse.TransferRequestId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClaimCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);
        await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        //This does not go well in the wallet since we haven't sent the certificate to the registry first,
        //and since the certificates does not appear in the wallet, but for this test we don't care
        var productionCert = new GranularCertificate
        {
            CertificateType = CertificateType.Production,
            Attributes = new Dictionary<string, string>(),
            End = new DateTimeOffset().AddHours(1).ToUnixTimeSeconds(),
            FederatedStreamId = new FederatedStreamId { Registry = "DK1", StreamId = Guid.NewGuid() },
            GridArea = "DK1",
            Quantity = 10,
            Start = new DateTimeOffset().ToUnixTimeSeconds()
        };
        var consumptionCert = new GranularCertificate
        {
            CertificateType = CertificateType.Consumption,
            Attributes = new Dictionary<string, string>(),
            End = new DateTimeOffset().AddHours(1).ToUnixTimeSeconds(),
            FederatedStreamId = new FederatedStreamId { Registry = "DK1", StreamId = Guid.NewGuid() },
            GridArea = "DK1",
            Quantity = 10,
            Start = new DateTimeOffset().ToUnixTimeSeconds()
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await walletClient.ClaimCertificatesAsync(ownerSubject, consumptionCert, productionCert,
                productionCert.Quantity, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task GetGranularCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);
        await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        //I cannot send any certificates to the wallet since I can't send to the registry first
        var certsResponse = await walletClient.GetGranularCertificatesAsync(ownerSubject, CancellationToken.None, limit: int.MaxValue, skip: 0);

        certsResponse.Should().NotBeNull();
        certsResponse!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClaims()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);
        await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        var claims = await walletClient.GetClaimsAsync(ownerSubject, null, null, CancellationToken.None);

        Assert.NotNull(claims);
        Assert.Empty(claims.Result);
    }

    [Fact]
    public async Task GetGranularCertificates_WhenNullLimit_ExpectNoErrors()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new WalletClient(httpClient);
        await walletClient.CreateWalletAsync(ownerSubject, CancellationToken.None);

        //I cannot send any certificates to the wallet since I can't send to the registry first
        var certsResponse = await walletClient.GetGranularCertificatesAsync(ownerSubject, CancellationToken.None, limit: null);

        certsResponse.Should().NotBeNull();
        certsResponse!.Result.Should().BeEmpty();
    }

    private HttpClient GetWalletHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(poStack.WalletUrl);

        return client;
    }

    private HttpClient GetStampHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(poStack.StampUrl);

        return client;
    }
}
