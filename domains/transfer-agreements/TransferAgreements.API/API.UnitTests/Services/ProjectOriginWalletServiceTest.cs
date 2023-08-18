using System;
using System.Threading.Tasks;
using API.Models;
using API.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;
using Xunit;

namespace API.UnitTests;

public class ProjectOriginWalletServiceTest
{
    private ProjectOriginWalletService service;
    private WalletService.WalletServiceClient fakeWalletServiceClient;

    public ProjectOriginWalletServiceTest()
    {
        var fakeLogger = Substitute.For<ILogger<ProjectOriginWalletService>>();
        fakeWalletServiceClient = Substitute.For<WalletService.WalletServiceClient>();

        service = new ProjectOriginWalletService(fakeLogger, fakeWalletServiceClient);
    }
    [Fact]
    public async Task TransferCertificates_TransferAgreementNoEndDate_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, SenderName = "Producent A/S",
            SenderTin = "32132112", ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse { GranularCertificates = { CreateGranularCertificate(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2))  } }
        );


        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() {}
        );

        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);

        fakeWalletServiceClient.Received(1).TransferCertificateAsync(Arg.Any<TransferRequest>(), Arg.Any<Metadata>());
    }

    [Fact]
    public async Task TransferCertificates_CertificateStartIsInvalidAndTANoEndDate_ShouldNotCallWalletTransferCertificate()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, SenderName = "Producent A/S",
            SenderTin = "32132112", ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse { GranularCertificates = { CreateGranularCertificate(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1))  } }
        );

        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() {}
        );

        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);

        fakeWalletServiceClient.Received(0).TransferCertificateAsync(Arg.Any<TransferRequest>(), Arg.Any<Metadata>());
    }

    [Fact]
    public async Task TransferCertificates_WhenCalled_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(3), SenderName = "Producent A/S",
            SenderTin = "32132112", ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse { GranularCertificates = { CreateGranularCertificate(DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2))  } }
        );

        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() {}
        );
        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);


        await service.TransferCertificates(transferAgreement);

        fakeWalletServiceClient.Received(1).TransferCertificateAsync(Arg.Any<TransferRequest>(), Arg.Any<Metadata>());
    }

    private void SetupWalletServiceClient(AsyncUnaryCall<QueryResponse> fakeGranularCertificatesResponse,
        AsyncUnaryCall<TransferResponse> fakeTransferResponse)
    {
        fakeWalletServiceClient.QueryGranularCertificatesAsync(
                Arg.Any<QueryRequest>(),
                Arg.Any<Metadata>())
            .Returns(fakeGranularCertificatesResponse);

        fakeWalletServiceClient.TransferCertificateAsync(
                Arg.Any<TransferRequest>(), Arg.Any<Metadata>())
            .Returns(fakeTransferResponse);
    }

    private static GranularCertificate CreateGranularCertificate(DateTimeOffset start, DateTimeOffset end)
    {
        return new GranularCertificate
        {
            Type = GranularCertificateType.Production,
            Start = Timestamp.FromDateTimeOffset(start),
            End = Timestamp.FromDateTimeOffset(end),
            FederatedId = new FederatedStreamId() { StreamId = new Uuid() { Value = Guid.NewGuid().ToString() } }
        };
    }

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
