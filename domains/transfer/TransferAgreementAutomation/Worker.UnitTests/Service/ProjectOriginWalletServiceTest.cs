using DataContext.Models;
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

    public ProjectOriginWalletServiceTest()
    {
        var fakeLogger = Substitute.For<ILogger<ProjectOriginWalletService>>();
        mockWalletClient = Substitute.For<IProjectOriginWalletClient>();
        var fakeMetrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        service = new ProjectOriginWalletService(fakeLogger, mockWalletClient, fakeMetrics);
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

    private static TransferAgreement CreateTransferAgreement(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var transferAgreement = new TransferAgreement
        {
            EndDate = endDate,
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "12345678",
            SenderId = Guid.NewGuid(),
            StartDate = startDate,
            Id = Guid.NewGuid(),
            SenderName = "SomeSender",
            SenderTin = "11223344",
            TransferAgreementNumber = 0
        };
        return transferAgreement;
    }

    private void SetupWalletServiceClient(List<GranularCertificate> mockedGranularCertificatesResponse,
        TransferResponse mockedTransferResponse)
    {
        mockWalletClient.GetGranularCertificates(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<uint?>()).Returns(
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
}
