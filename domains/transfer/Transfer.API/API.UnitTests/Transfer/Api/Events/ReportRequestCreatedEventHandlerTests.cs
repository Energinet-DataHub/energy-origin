using System;
using System.Threading;
using System.Threading.Tasks;
using API.Events;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api.Events;

public class ReportRequestCreatedEventHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ReportRequestCreatedEventHandler> _logger = Substitute.For<ILogger<ReportRequestCreatedEventHandler>>();
    private readonly ReportRequestCreatedEventHandler _sut;

    public ReportRequestCreatedEventHandlerTests()
    {
        _uow.SaveAsync().Returns(Task.CompletedTask);

        _sut = new ReportRequestCreatedEventHandler(
            _mediator, _logger, _reports, _uow
        );
    }

    [Fact]
    public async Task Consume_SuccessfulPdfGeneration_MarksReportCompleted()
    {
        var reportId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var @event = new ReportRequestCreated(reportId, start, end);

        var report = Report.Create(
            OrganizationId.Create(Guid.NewGuid()),
            UnixTimestamp.Create(start),
            UnixTimestamp.Create(end)
        );
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>())
            .Returns(report);

        var pdfBytes = new byte[] { 1, 2, 3 };
        _mediator.Send(Arg.Any<GeneratePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratePdfResult(true, PdfBytes: pdfBytes));

        var context = Substitute.For<ConsumeContext<ReportRequestCreated>>();
        context.Message.Returns(@event);
        context.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(context);

        report.Status.Should().Be(ReportStatus.Completed);
        report.Content.Should().Equal(pdfBytes);
        await _reports.Received(1).UpdateAsync(report, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveAsync();

        await _mediator.DidNotReceive().Publish(
            Arg.Any<object>(), Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Consume_PdfGenerationFails_MarksReportFailed()
    {
        var reportId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var @event = new ReportRequestCreated(reportId, start, end);

        var report = Report.Create(
            OrganizationId.Create(Guid.NewGuid()),
            UnixTimestamp.Create(start),
            UnixTimestamp.Create(end)
        );
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>())
            .Returns(report);

        _mediator.Send(Arg.Any<GeneratePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratePdfResult(
                IsSuccess: false,
                StatusCode: 400,
                ErrorContent: "oops"
            ));

        var context = Substitute.For<ConsumeContext<ReportRequestCreated>>();
        context.Message.Returns(@event);
        context.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(context);

        report.Status.Should().Be(ReportStatus.Failed);
        report.Content.Should().BeNull();
        await _reports.Received(1).UpdateAsync(report, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveAsync();

        await _mediator.DidNotReceive().Publish(
            Arg.Any<object>(), Arg.Any<CancellationToken>()
        );
    }
}
