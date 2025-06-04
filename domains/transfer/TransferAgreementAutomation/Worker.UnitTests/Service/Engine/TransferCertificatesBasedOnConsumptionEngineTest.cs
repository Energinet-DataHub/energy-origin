using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Service.Engine;
using TransferAgreementAutomation.Worker.Service.TransactionStatus;
using Xunit;
using RequestStatus = TransferAgreementAutomation.Worker.Service.TransactionStatus.RequestStatus;

namespace Worker.UnitTests.Service.Engine;

public class TransferCertificatesBasedOnConsumptionEngineTest
{
    private readonly IWalletClient mockWalletClient;
    private readonly InMemoryRequestStatusRepository requestStatusStore;

    private readonly int batchSize = 2;
    private readonly TransferCertificatesBasedOnConsumptionEngine sut;

    public TransferCertificatesBasedOnConsumptionEngineTest()
    {
        var fakeLogger = Substitute.For<ILogger<TransferCertificatesBasedOnConsumptionEngine>>();
        mockWalletClient = Substitute.For<IWalletClient>();
        var fakeMetrics = Substitute.For<ITransferAgreementAutomationMetrics>();
        requestStatusStore = new InMemoryRequestStatusRepository();
        var transferEngineUtility = new TransferEngineUtility(mockWalletClient, requestStatusStore, NullLogger<TransferEngineUtility>.Instance)
        { BatchSize = batchSize };

        sut = new TransferCertificatesBasedOnConsumptionEngine(requestStatusStore, transferEngineUtility, fakeLogger, mockWalletClient);
    }

    [Fact]
    public async Task GivenTransferAgreementWithoutReceiverId_WhenTransferring_AgreementIsSkipped()
    {
        // Given no receiver id
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null, null);

        // When transferring
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then transfer agreement is skipped (no certificates fetched)
        await mockWalletClient
            .DidNotReceive()
            .GetGranularCertificates(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task GivenPendingSenderTransaction_WhenTransferring_AgreementIsSkipped()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, receiverId, null);

        // When transferring
        await requestStatusStore.Add(new RequestStatus(transferAgreement.SenderId, OrganizationId.Empty(), Guid.NewGuid(),
            UnixTimestamp.Now().AddMinutes(-2)), TestContext.Current.CancellationToken);
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then transfer agreement is skipped (no certificates fetched)
        await mockWalletClient
            .DidNotReceive()
            .GetGranularCertificates(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task GivenPendingReceiverTransaction_WhenTransferring_AgreementIsSkipped()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, receiverId, null);

        // When transferring
        await requestStatusStore.Add(new RequestStatus(Any.OrganizationId(), receiverId, Guid.NewGuid(), UnixTimestamp.Now().AddMinutes(-2)), TestContext.Current.CancellationToken);
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then transfer agreement is skipped (no certificates fetched)
        await mockWalletClient
            .DidNotReceive()
            .GetGranularCertificates(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task GivenTimedOutTransaction_WhenTransferring_CertificatesAreTransferred()
    {
        // Given transfer agreement and a timed out transaction
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, receiverId, null);
        await requestStatusStore.Add(new RequestStatus(Any.OrganizationId(), receiverId, Guid.NewGuid(), UnixTimestamp.Now().AddDays(-2)), TestContext.Current.CancellationToken);

        // When transferring
        var cert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production);
        SetupWalletServiceClient(receiverId.Value, [cert], new TransferResponse() { TransferRequestId = Guid.NewGuid() });
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then certificates are transferred
        await mockWalletClient
            .Received(1)
            .GetGranularCertificates(
                Arg.Is(receiverId.Value),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task GivenTransferAgreement_WhenReceiverHasNoConsumptionCertificates_NoCertificatesAreTransferred()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, receiverId, null);

        // When transferring
        SetupWalletServiceClient(transferAgreement.ReceiverId!.Value,
            [Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production)],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then no certificates are transferred
        await mockWalletClient
            .DidNotReceive()
            .GetGranularCertificates(
                Arg.Is(transferAgreement.SenderId.Value),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
        var transactions = await requestStatusStore.GetByOrganization(transferAgreement.SenderId, CancellationToken.None);
        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenTransferAgreement_WhenReceiverHasUnmatchedConsumptionCertificates_SenderCertificatesAreFetched()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow.AddHours(-10), receiverId, DateTimeOffset.UtcNow);

        // When transferring
        var receiverConsumptionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Consumption, 11);
        var receiverProductionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 5);
        SetupWalletServiceClient(transferAgreement.ReceiverId!.Value, [receiverConsumptionCert, receiverProductionCert],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        var senderProductionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 6);
        SetupWalletServiceClient(transferAgreement.SenderId.Value, [senderProductionCert],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        // Then certificates are transferred
        var transactions = await requestStatusStore.GetByOrganization(transferAgreement.SenderId, CancellationToken.None);
        transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenTransferAgreement_WhenSenderHasSurplusOfProductionCertificates_SubsetOfSenderCertificatesAreTransferred()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow.AddHours(-10), receiverId, null);

        // When transferring
        var receiverConsumptionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Consumption, 11);
        SetupWalletServiceClient(transferAgreement.ReceiverId!.Value, [receiverConsumptionCert],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        var senderProductionCert1 = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 3);
        var senderProductionCert2 = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 4);
        var senderProductionCert3 = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 5);
        var senderProductionCert4 = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 6);
        SetupWalletServiceClient(transferAgreement.SenderId.Value,
            [senderProductionCert1, senderProductionCert2, senderProductionCert3, senderProductionCert4],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        // Then certificates are transferred
        var transactions = await requestStatusStore.GetByOrganization(transferAgreement.SenderId, CancellationToken.None);
        transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GivenCertificateOutsideTransactionAgreementPeriod_WhenTransferringCertificates_NoCertificatesAreTransferred()
    {
        // Given transfer agreement
        var receiverId = Any.OrganizationId();
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, receiverId, DateTimeOffset.UtcNow.AddDays(1));

        // When transferring
        var receiverConsumptionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Consumption, 11);
        SetupWalletServiceClient(transferAgreement.ReceiverId!.Value, [receiverConsumptionCert],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        var senderProductionCert = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-5), CertificateType.Production, 3);
        SetupWalletServiceClient(transferAgreement.SenderId.Value, [senderProductionCert],
            new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then certificates are transferred
        var transactions = await requestStatusStore.GetByOrganization(transferAgreement.SenderId, CancellationToken.None);
        transactions.Should().BeEmpty();
    }

    private static TransferAgreement CreateTransferAgreement(DateTimeOffset startDate, OrganizationId? receiverId, DateTimeOffset? endDate)
    {
        var transferAgreement = new TransferAgreement
        {
            EndDate = endDate is null ? null : UnixTimestamp.Create(endDate.Value),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = Tin.Create("12345678"),
            SenderId = OrganizationId.Create(Guid.NewGuid()),
            StartDate = UnixTimestamp.Create(startDate),
            Id = Guid.NewGuid(),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = Tin.Create("11223344"),
            TransferAgreementNumber = 0,
            Type = TransferAgreementType.TransferCertificatesBasedOnConsumption,
            ReceiverId = receiverId
        };
        return transferAgreement;
    }

    private void SetupWalletServiceClient(Guid owner, List<GranularCertificate> mockedGranularCertificatesResponse,
        TransferResponse mockedTransferResponse)
    {
        var certificateCount = mockedGranularCertificatesResponse.Count;
        mockWalletClient.GetGranularCertificates(Arg.Is(owner), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>(),
            Arg.Any<CertificateType>()).Returns(
            new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = certificateCount, Limit = 100, Total = certificateCount },
                Result = mockedGranularCertificatesResponse
            });

        mockWalletClient.GetGranularCertificates(Arg.Is(owner), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>()).Returns(
            new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = certificateCount, Limit = 100, Total = certificateCount },
                Result = mockedGranularCertificatesResponse
            });

        mockWalletClient
            .TransferCertificates(Arg.Is(owner), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>(), CancellationToken.None)
            .Returns(mockedTransferResponse);
    }
}
