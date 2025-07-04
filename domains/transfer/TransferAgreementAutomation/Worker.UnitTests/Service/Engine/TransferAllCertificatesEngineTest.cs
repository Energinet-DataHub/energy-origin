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

namespace Worker.UnitTests.Service.Engine;

public class TransferAllCertificatesEngineTest
{
    private readonly TransferAllCertificatesEngine sut;
    private readonly IWalletClient mockWalletClient;
    private readonly InMemoryRequestStatusRepository requestStatusStore;

    private readonly int batchSize = 2;

    public TransferAllCertificatesEngineTest()
    {
        var fakeLogger = Substitute.For<ILogger<TransferAllCertificatesEngine>>();
        mockWalletClient = Substitute.For<IWalletClient>();
        var fakeMetrics = Substitute.For<ITransferAgreementAutomationMetrics>();
        requestStatusStore = new InMemoryRequestStatusRepository();
        var transferEngineUtility = new TransferEngineUtility(mockWalletClient, requestStatusStore, NullLogger<TransferEngineUtility>.Instance)
        { BatchSize = batchSize };

        sut = new TransferAllCertificatesEngine(requestStatusStore, fakeLogger, mockWalletClient, fakeMetrics, transferEngineUtility);
    }

    [Fact]
    public async Task TransferCertificates_TransferAgreementNoEndDate_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(1)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                cert,
                cert.Quantity,
                transferAgreement.ReceiverReference,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GivenCertificate_WhenTransferring_RequestStateIsUpdated()
    {
        // Given certificate
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow.AddHours(-5), null);
        var certificate = Any.GranularCertificate(UnixTimestamp.Now().AddHours(-2), CertificateType.Production);
        SetupWalletServiceClient([certificate], new TransferResponse { TransferRequestId = Guid.NewGuid() });

        // When transferring certificate
        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        // Then request status is updated
        var transactions = await requestStatusStore.GetByOrganization(transferAgreement.SenderId, CancellationToken.None);
        transactions.Should().HaveCount(1);
        transactions.First().Status.Should().Be(Status.Pending);
        transactions.First().SenderId.Should().Be(transferAgreement.SenderId);
    }

    [Fact]
    public async Task GivenPendingTransactions_WhenTransferring_TransferAgreementIsSkipped()
    {
        // Given pending transaction
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow.AddHours(-5), null);
        await requestStatusStore.Add(new(transferAgreement.SenderId, OrganizationId.Empty(), Guid.NewGuid(), UnixTimestamp.Now()), TestContext.Current.CancellationToken);

        // When transferring certificate
        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        // Then transfer agreement is skipped (no certificates fetched)
        await mockWalletClient
            .DidNotReceive()
            .GetGranularCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task TransferCertificates_CertificateStartDateBeforeTAStartDateAndTANoEndDate_ShouldNotCallWalletTransferCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        var transferAgreement = CreateTransferAgreement(now, now.AddHours(3));

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1));

        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        _ = mockWalletClient
            .DidNotReceive()
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                Arg.Any<Guid>(),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task
        TransferCertificates_CertificateEndDateAfterTAStartDatCertificateStartDateIsBefore_ShouldNotCallWalletTransferCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        var transferAgreement = CreateTransferAgreement(now, now.AddHours(3));

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, TestContext.Current.CancellationToken);

        _ = mockWalletClient
            .DidNotReceive()
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                Arg.Any<Guid>(),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenCalled_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3));

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));

        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(1)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                cert,
                cert.Quantity,
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenCalledWithTrialTransferAgreementAndTrialCertificates_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3), isTrial: true);

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2), isTrial: true);

        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });
        sut.SetEngineTrialState(transferAgreement);

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(1)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                cert,
                cert.Quantity,
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenCalledWithTrialTransferAgreementAndNoTrialCertificates_ShouldNotCallWalletTransferCertificate()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3), isTrial: true);
        sut.SetEngineTrialState(transferAgreement);

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));

        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .DidNotReceive()
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                cert,
                cert.Quantity,
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task TransferCertificates_WhenHittingBatchSize_TransferAll(int numberOfCerts)
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var certs = Any.GranularCertificatesList(numberOfCerts, UnixTimestamp.Now().AddHours(1));

        var results = new List<ResultList<GranularCertificate>>();
        if (!certs.Any())
        {
            results.Add(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 0, Limit = batchSize, Total = 0 },
                Result = []
            });
        }
        else
        {
            decimal nn2 = Decimal.Divide(numberOfCerts, batchSize);
            var nn3 = (int)Math.Ceiling(nn2);
            for (int i = 0; i < nn3; i++)
            {
                results.Add(new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo()
                    { Offset = 0, Count = certs.Skip(i * batchSize).Take(batchSize).Count(), Limit = batchSize, Total = certs.Count },
                    Result = certs.Skip(i * batchSize).Take(batchSize)
                });
            }
        }

        mockWalletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>(),
                Arg.Any<CertificateType>())
            .Returns(results[0], results.Skip(1).ToArray());

        mockWalletClient
            .TransferCertificatesAsync(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>(), CancellationToken.None)
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(numberOfCerts)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenTotalDecreasesOnNextCall_TransferAll()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var certs = Any.GranularCertificatesList(4, UnixTimestamp.Now().AddHours(1));

        mockWalletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>(),
                Arg.Any<CertificateType>())
            .Returns(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 4 },
                Result = certs.Take(2)
            },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 3 },
                    Result = [certs[2]]
                });

        mockWalletClient
            .TransferCertificatesAsync(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>(), CancellationToken.None)
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(3)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenTotalIncreasesOnNextCall_TransferAll()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var certs = Any.GranularCertificatesList(5, UnixTimestamp.Now().AddHours(1));

        mockWalletClient.GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>(),
                Arg.Any<CertificateType>())
            .Returns(new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 4 },
                Result = certs.Take(2)
            },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 2, Limit = batchSize, Total = 5 },
                    Result = certs.Skip(2).Take(2)
                },
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 1, Limit = batchSize, Total = 5 },
                    Result = certs.Skip(4).Take(1)
                });

        mockWalletClient
            .TransferCertificatesAsync(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>(), CancellationToken.None)
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await sut.TransferCertificates(transferAgreement, CancellationToken.None);

        _ = mockWalletClient
            .Received(5)
            .TransferCertificatesAsync(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference,
                CancellationToken.None
            );
    }

    private static TransferAgreement CreateTransferAgreement(DateTimeOffset startDate, DateTimeOffset? endDate, bool isTrial = false)
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
            Type = TransferAgreementType.TransferAllCertificates,
            IsTrial = isTrial,
        };
        return transferAgreement;
    }

    private void SetupWalletServiceClient(List<GranularCertificate> mockedGranularCertificatesResponse, TransferResponse mockedTransferResponse)
    {
        mockWalletClient
            .GetGranularCertificatesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<CertificateType>())
            .Returns(
                new ResultList<GranularCertificate>()
                {
                    Metadata = new PageInfo() { Offset = 0, Count = 1, Limit = 100, Total = 1 },
                    Result = mockedGranularCertificatesResponse
                });

        mockWalletClient
            .TransferCertificatesAsync(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>(), CancellationToken.None)
            .Returns(mockedTransferResponse);
    }

    private static GranularCertificate CreateGranularCertificate(DateTimeOffset start, DateTimeOffset end, bool isTrial = false)
    {
        return new GranularCertificate
        {
            CertificateType = CertificateType.Production,
            Start = start.ToUnixTimeSeconds(),
            End = end.ToUnixTimeSeconds(),
            FederatedStreamId = new FederatedStreamId() { Registry = "DK1", StreamId = Guid.NewGuid() },
            GridArea = "DK1",
            Quantity = 123,
            Attributes = new Dictionary<string, string>() { { "IsTrial", isTrial.ToString() } }
        };
    }
}
