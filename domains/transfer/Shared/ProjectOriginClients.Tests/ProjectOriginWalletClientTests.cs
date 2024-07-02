using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ProjectOriginClients.Models;
using ProjectOriginClients.Tests.Testcontainers;
using Xunit;

namespace ProjectOriginClients.Tests;

public class ProjectOriginWalletClientTests : IClassFixture<ProjectOriginStack>
{
    private readonly ProjectOriginStack poStack;

    public ProjectOriginWalletClientTests(ProjectOriginStack poStack)
    {
        this.poStack = poStack;
    }

    [Fact]
    public async Task CreateAndGetWallets()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null); // Do we want to test Proxy here?

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var wallets = await walletClient.GetWallets(ownerSubject, new CancellationToken());

        wallets.Should().NotBeNull();
        wallets.Result.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetWallets_WhenNoWallets_ExpectEmptyResult()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        var wallets = await walletClient.GetWallets(ownerSubject, new CancellationToken());

        wallets.Should().NotBeNull();
        wallets.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWalletEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpoint(ownerSubject, createWalletResponse.WalletId, new CancellationToken());

        walletEndpoint.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateExternalEndpoint()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpoint(ownerSubject, createWalletResponse.WalletId, new CancellationToken());

        walletEndpoint.Should().NotBeNull();
        var cvrNumber = "12345678";
        var externalEndpoint = await walletClient.CreateExternalEndpoint(Guid.NewGuid(), walletEndpoint, cvrNumber, new CancellationToken());

        externalEndpoint.Should().NotBeNull();
        externalEndpoint.ReceiverId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TransferCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        var createWalletResponse = await walletClient.CreateWallet(ownerSubject, new CancellationToken());

        createWalletResponse.Should().NotBeNull();

        var walletEndpoint = await walletClient.CreateWalletEndpoint(ownerSubject, createWalletResponse.WalletId, new CancellationToken());

        walletEndpoint.Should().NotBeNull();
        var cvrNumber = "12345678";
        var externalEndpoint = await walletClient.CreateExternalEndpoint(Guid.NewGuid(), walletEndpoint, cvrNumber, new CancellationToken());

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
        var transferCertificatesResponse = await walletClient.TransferCertificates(ownerSubject, cert, cert.Quantity, externalEndpoint.ReceiverId);

        transferCertificatesResponse.Should().NotBeNull();
        transferCertificatesResponse.TransferRequestId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClaimCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

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

        var claimResponse = await walletClient.ClaimCertificates(ownerSubject, consumptionCert, productionCert, productionCert.Quantity);

        claimResponse.Should().NotBeNull();
        claimResponse.ClaimRequestId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetGranularCertificates()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        //I cannot send any certificates to the wallet since I can't send to the registry first
        var certsResponse = await walletClient.GetGranularCertificates(ownerSubject, new CancellationToken(), limit: int.MaxValue, skip: 0);

        certsResponse.Should().NotBeNull();
        certsResponse!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGranularCertificates_WhenNullLimit_ExpectNoErrors()
    {
        var ownerSubject = Guid.NewGuid();
        var httpClient = GetWalletHttpClient();
        var walletClient = new ProjectOriginWalletClient(httpClient, null);

        //I cannot send any certificates to the wallet since I can't send to the registry first
        var certsResponse = await walletClient.GetGranularCertificates(ownerSubject, new CancellationToken(), limit: null);

        certsResponse.Should().NotBeNull();
        certsResponse!.Result.Should().BeEmpty();
    }

    private HttpClient GetWalletHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(poStack.WalletUrl);

        return client;
    }
}
