using System;
using System.Threading.Tasks;
using API.Shared.Services;
using API.Transfer.Api.Models;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProjectOrigin.Common.V1;
using ProjectOrigin.WalletSystem.V1;
using Xunit;

namespace API.UnitTests.Transfer.Api.Services;

public class ProjectOriginWalletServiceTest
{
    private readonly ProjectOriginWalletService service;
    private readonly WalletService.WalletServiceClient fakeWalletServiceClient;
    private readonly ITransferAgreementAutomationMetrics fakeMetrics;

    public ProjectOriginWalletServiceTest()
    {
        var fakeLogger = Substitute.For<ILogger<ProjectOriginWalletService>>();
        fakeWalletServiceClient = Substitute.For<WalletService.WalletServiceClient>();
        fakeMetrics = Substitute.For<ITransferAgreementAutomationMetrics>();
        var fakeCache = new AutomationCache();

        service = new ProjectOriginWalletService(fakeLogger, fakeWalletServiceClient, fakeMetrics, fakeCache);
    }

    [Fact]
    public async Task TransferCertificates_CertificateNotTransfer_ShouldSetMetric()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            SenderName = "Producent A/S",
            SenderTin = "32132112",
            ReceiverTin = "11223344"
        };

        var certificate = new GranularCertificate
        {
            Type = GranularCertificateType.Production,
            Start = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1)),
            End = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(2)),
            FederatedId = new FederatedStreamId() { StreamId = new Uuid() { Value = Guid.NewGuid().ToString() } }
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse { GranularCertificates = { certificate } }
        );
        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() { }
        );
        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);
        fakeMetrics.DidNotReceive().AddTransferError();

        await service.TransferCertificates(transferAgreement);
        fakeMetrics.Received(1).AddTransferError();
    }

    [Fact]
    public async Task TransferCertificates_TransferAgreementNoEndDate_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            SenderName = "Producent A/S",
            SenderTin = "32132112",
            ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse
            {
                GranularCertificates =
                    { CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2)) }
            }
        );


        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() { }
        );

        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);

        _ = fakeWalletServiceClient
               .Received(1)
               .TransferCertificateAsync(
                   Arg.Any<TransferRequest>(),
                   Arg.Is<Metadata>(x => x.Get("Authorization")!.Value.StartsWith("Bearer "))
               );
    }

    [Fact]
    public async Task
        TransferCertificates_CertificateStartDateBeforeTAStartDateAndTANoEndDate_ShouldNotCallWalletTransferCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = now,
            SenderName = "Producent A/S",
            SenderTin = "32132112",
            ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse
            { GranularCertificates = { CreateGranularCertificate(now.AddHours(-2), now.AddHours(-1)) } }
        );

        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() { }
        );

        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);

        _ = fakeWalletServiceClient
              .DidNotReceive()
              .TransferCertificateAsync(
                  Arg.Any<TransferRequest>(),
                  Arg.Is<Metadata>(x => x.Get("Authorization")!.Value.StartsWith("Bearer "))
              );
    }

    [Fact]
    public async Task
        TransferCertificates_CertificateEndDateAfterTAStartDatCertificateStartDateIsBefore_ShouldNotCallWalletTransferCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = now,
            EndDate = now.AddHours(3),
            SenderName = "Producent A/S",
            SenderTin = "32132112",
            ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse
            { GranularCertificates = { CreateGranularCertificate(now.AddHours(-1), now.AddHours(1)) } }
        );

        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() { }
        );

        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);

        await service.TransferCertificates(transferAgreement);

        _ = fakeWalletServiceClient
             .DidNotReceive()
             .TransferCertificateAsync(
                 Arg.Any<TransferRequest>(),
                 Arg.Is<Metadata>(x => x.Get("Authorization")!.Value.StartsWith("Bearer "))
             );
    }

    [Fact]
    public async Task TransferCertificates_WhenCalled_ShouldCallWalletTransferCertificate()
    {
        var transferAgreement = new TransferAgreement()
        {
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(3),
            SenderName = "Producent A/S",
            SenderTin = "32132112",
            ReceiverTin = "11223344"
        };

        var fakeGranularCertificatesResponse = CreateAsyncUnaryCall(
            new QueryResponse
            {
                GranularCertificates =
                    { CreateGranularCertificate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2)) }
            }
        );

        var fakeTransferResponse = CreateAsyncUnaryCall(
            new TransferResponse() { }
        );
        SetupWalletServiceClient(fakeGranularCertificatesResponse, fakeTransferResponse);


        await service.TransferCertificates(transferAgreement);

        _ = fakeWalletServiceClient
             .Received(1)
             .TransferCertificateAsync(
                 Arg.Any<TransferRequest>(),
                 Arg.Is<Metadata>(x => x.Get("Authorization")!.Value.StartsWith("Bearer "))
             );
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
