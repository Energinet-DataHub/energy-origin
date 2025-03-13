using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TransferAgreementAutomation.Worker.Service.Engine;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using Xunit;
using RequestStatus = TransferAgreementAutomation.Worker.Service.TransactionStatus.RequestStatus;

namespace Worker.UnitTests.Service.Engine;

public class TransferEngineUtilityTest
{
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IWalletClient _mockWalletClient;
    private readonly TransferEngineUtility _sut;

    public TransferEngineUtilityTest()
    {
        _requestStatusRepository = new InMemoryRequestStatusRepository();
        _mockWalletClient = Substitute.For<IWalletClient>();
        _sut = new TransferEngineUtility(_mockWalletClient, _requestStatusRepository, NullLogger<TransferEngineUtility>.Instance);
    }

    [Fact]
    public async Task GivenNoCertificates_WhenFetching_EmptyListIsReturned()
    {
        // Given no certificates
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var result = new ResultList<GranularCertificate>()
        { Result = new List<GranularCertificate>(), Metadata = new PageInfo() { Count = 0, Offset = 0, Total = 0, Limit = 0 } };
        _mockWalletClient.GetGranularCertificates(orgId.Value, Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(result);

        // When fetching
        var certificates = await _sut.GetCertificates(orgId);

        // No certificates is returned
        certificates.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenMultiplePagesWithCertificates_WhenFetching_AllPagesAreFetchedAndReturned()
    {
        // Given no certificates
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var result1 = new ResultList<GranularCertificate>()
        {
            Result = Enumerable.Range(0, 2000).Select(_ => Any.GranularCertificate()),
            Metadata = new PageInfo() { Count = 2000, Offset = 0, Total = 3000, Limit = 0 }
        };
        var result2 = new ResultList<GranularCertificate>()
        {
            Result = Enumerable.Range(0, 1000).Select(_ => Any.GranularCertificate()),
            Metadata = new PageInfo() { Count = 1000, Offset = 2000, Total = 3000, Limit = 0 }
        };
        _mockWalletClient.GetGranularCertificates(orgId.Value, Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>())
            .Returns(result1, result2);

        // When fetching
        var certificates = await _sut.GetCertificates(orgId);

        // All certificates are returned
        certificates.Should().HaveCount(3000);
    }

    [Fact]
    public async Task GivenPendingTransactions_WhenChecking_OrganizationHasPendingTransactions()
    {
        // Given pending transaction
        var orgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(orgId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now()), TestContext.Current.CancellationToken);

        // When checking for pending transactions
        var hasPendingTransactions = await _sut.HasPendingTransactions(orgId, TestContext.Current.CancellationToken);

        // Then has pending
        hasPendingTransactions.Should().BeTrue();
    }

    [Fact]
    public async Task GivenPendingTransactions_WhenCheckingForReceiverOrg_OrganizationHasPendingTransactions()
    {
        // Given pending transaction
        var senderOrgId = OrganizationId.Create(Guid.NewGuid());
        var receiverOrgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(senderOrgId, receiverOrgId, Guid.NewGuid(), UnixTimestamp.Now()), TestContext.Current.CancellationToken);

        // When checking for pending transactions
        var hasPendingTransactions = await _sut.HasPendingTransactions(receiverOrgId, TestContext.Current.CancellationToken);

        // Then has pending
        hasPendingTransactions.Should().BeTrue();
    }

    [Fact]
    public async Task GivenOlderPendingTransactions_WhenCheckingStatus_StatusIsUpdated()
    {
        // Given pending transaction and updated status
        var orgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(orgId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now().AddMinutes(-3)), TestContext.Current.CancellationToken);
        var request = (await _requestStatusRepository.GetByOrganization(orgId, TestContext.Current.CancellationToken)).Single();
        _mockWalletClient.GetRequestStatus(Arg.Is(request.SenderId.Value), Arg.Is(request.RequestId), Arg.Any<CancellationToken>())
            .Returns(EnergyOrigin.WalletClient.RequestStatus.Completed);

        // When checking for pending transactions
        var hasPendingTransactions = await _sut.HasPendingTransactions(orgId, TestContext.Current.CancellationToken);

        // Then no pending transactions
        await _mockWalletClient.Received(1).GetRequestStatus(Arg.Is(request.SenderId.Value), Arg.Is(request.RequestId), Arg.Any<CancellationToken>());
        hasPendingTransactions.Should().BeFalse();
    }

    [Fact]
    public async Task GivenVeryOldCompletedTransactions_WhenCheckingStatus_StatusIsNotUpdated()
    {
        // Given completed transaction
        var orgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(orgId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now().AddDays(-3),
            Status.Completed), TestContext.Current.CancellationToken);

        // When checking for transaction status
        var hasPendingTransactions = await _sut.HasPendingTransactions(orgId, TestContext.Current.CancellationToken);

        // Then no pending transactions
        await _mockWalletClient.Received(0).GetRequestStatus(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        hasPendingTransactions.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOldTransaction_WhenUpdatingStatus_TransactionIsTimedOut()
    {
        // Given old pending transaction
        var orgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(orgId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now().AddHours(-3)), TestContext.Current.CancellationToken);

        // When checking for transaction status
        var hasPendingTransactions = await _sut.HasPendingTransactions(orgId, TestContext.Current.CancellationToken);

        // Transaction is timed out, and no pending transactions
        await _mockWalletClient.Received(0).GetRequestStatus(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        hasPendingTransactions.Should().BeFalse();
        (await _requestStatusRepository.GetByOrganization(orgId, TestContext.Current.CancellationToken)).SingleOrDefault()!.Status.Should().Be(Status.Timeout);
    }

    [Fact]
    public async Task GivenVeryOldTransaction_WhenUpdatingStatus_TransactionStatusIsRemoved()
    {
        // Given very old pending transaction
        var orgId = OrganizationId.Create(Guid.NewGuid());
        await _requestStatusRepository.Add(new RequestStatus(orgId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now().AddDays(-3)), TestContext.Current.CancellationToken);

        // When checking for transaction status
        var hasPendingTransactions = await _sut.HasPendingTransactions(orgId, TestContext.Current.CancellationToken);

        // Transaction is deleted, and no pending transactions
        await _mockWalletClient.Received(0).GetRequestStatus(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        hasPendingTransactions.Should().BeFalse();
        (await _requestStatusRepository.GetByOrganization(orgId, TestContext.Current.CancellationToken)).SingleOrDefault().Should().BeNull();
    }
}
