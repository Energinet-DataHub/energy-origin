using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOriginClients;
using ProjectOriginClients.Models;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Service;
using Xunit;

namespace Worker.UnitTests.Service;

public class ProjectOriginWalletServiceTest
{
    private readonly ProjectOriginWalletService service;
    private readonly IProjectOriginWalletClient mockWalletClient;

    private readonly int batchSize = 2;

    public ProjectOriginWalletServiceTest()
    {
        var fakeLogger = Substitute.For<ILogger<ProjectOriginWalletService>>();
        mockWalletClient = Substitute.For<IProjectOriginWalletClient>();
        var fakeMetrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        service = new ProjectOriginWalletService(fakeLogger, mockWalletClient, fakeMetrics) { BatchSize = batchSize };
    }

    [Fact]
    public async Task TransferCertificates_TransferAgreementNoEndDate_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3));

        var cert = CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        SetupWalletServiceClient(
            [cert],
            new TransferResponse { TransferRequestId = Guid.NewGuid() });

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
               .Received(1)
               .TransferCertificates(
                   Arg.Any<Guid>(),
                   cert,
                   cert.Quantity,
                   transferAgreement.ReceiverReference
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

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
              .DidNotReceive()
              .TransferCertificates(
                  Arg.Any<Guid>(),
                  Arg.Any<GranularCertificate>(),
                  Arg.Any<uint>(),
                  Arg.Any<Guid>()
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

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
             .DidNotReceive()
             .TransferCertificates(
                 Arg.Any<Guid>(),
                 Arg.Any<GranularCertificate>(),
                 Arg.Any<uint>(),
                 Arg.Any<Guid>()
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

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
             .Received(1)
             .TransferCertificates(
                 Arg.Any<Guid>(),
                 cert,
                 cert.Quantity,
                 transferAgreement.ReceiverReference
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

        var certs = CreateGranularCertificates(numberOfCerts);

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
                    Metadata = new PageInfo() { Offset = 0, Count = certs.Skip(i * batchSize).Take(batchSize).Count(), Limit = batchSize, Total = certs.Count },
                    Result = certs.Skip(i * batchSize).Take(batchSize)
                });
            }
        }

        mockWalletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>()).Returns(results[0], results.Skip(1).ToArray());

        mockWalletClient
            .TransferCertificates(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>())
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
            .Received(numberOfCerts)
            .TransferCertificates(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenTotalDecreasesOnNextCall_TransferAll()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var certs = CreateGranularCertificates(4);

        mockWalletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>())
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
            .TransferCertificates(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>())
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
            .Received(3)
            .TransferCertificates(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference
            );
    }

    [Fact]
    public async Task TransferCertificates_WhenTotalIncreasesOnNextCall_TransferAll()
    {
        var transferAgreement = CreateTransferAgreement(DateTimeOffset.UtcNow, null);

        var certs = CreateGranularCertificates(5);

        mockWalletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>(), skip: Arg.Any<int>())
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
            .TransferCertificates(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>())
            .Returns(new TransferResponse() { TransferRequestId = Guid.NewGuid() });

        await service.TransferCertificates(transferAgreement);

        _ = mockWalletClient
            .Received(5)
            .TransferCertificates(
                Arg.Any<Guid>(),
                Arg.Any<GranularCertificate>(),
                Arg.Any<uint>(),
                transferAgreement.ReceiverReference
            );
    }
    private static TransferAgreement CreateTransferAgreement(DateTimeOffset startDate, DateTimeOffset? endDate)
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
            TransferAgreementNumber = 0
        };
        return transferAgreement;
    }

    private void SetupWalletServiceClient(List<GranularCertificate> mockedGranularCertificatesResponse,
        TransferResponse mockedTransferResponse)
    {
        mockWalletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<int?>()).Returns(
            new ResultList<GranularCertificate>()
            {
                Metadata = new PageInfo() { Offset = 0, Count = 1, Limit = 100, Total = 1 },
                Result = mockedGranularCertificatesResponse
            });

        mockWalletClient
            .TransferCertificates(Arg.Any<Guid>(), Arg.Any<GranularCertificate>(), Arg.Any<uint>(), Arg.Any<Guid>())
            .Returns(mockedTransferResponse);
    }

    private static GranularCertificate CreateGranularCertificate(DateTimeOffset start, DateTimeOffset end)
    {
        return new GranularCertificate
        {
            CertificateType = CertificateType.Production,
            Start = start.ToUnixTimeSeconds(),
            End = end.ToUnixTimeSeconds(),
            FederatedStreamId = new FederatedStreamId() { Registry = "DK1", StreamId = Guid.NewGuid() },
            GridArea = "DK1",
            Quantity = 123,
            Attributes = new Dictionary<string, string>()
        };
    }

    private List<GranularCertificate> CreateGranularCertificates(int count)
    {
        var certs = new List<GranularCertificate>();
        for (var i = 0; i < count; i++)
        {
            certs.Add(CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(i), DateTimeOffset.UtcNow.AddHours(i + 1)));
        }

        return certs;
    }
}
