using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ReportGenerator.Domain;
using API.ReportGenerator.Infrastructure;
using API.ReportGenerator.Processing;
using API.ReportGenerator.Rendering;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class CreateReportRequestCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<CreateReportRequestCommandHandler> _logger = Substitute.For<ILogger<CreateReportRequestCommandHandler>>();
    private readonly IEnergyDataFetcher _dataFetcher = Substitute.For<IEnergyDataFetcher>();
    private readonly IHeadlinePercentageProcessor _percentageProcessor = Substitute.For<IHeadlinePercentageProcessor>();
    private readonly IEnergySvgRenderer _svgRenderer = Substitute.For<IEnergySvgRenderer>();
    private readonly IOrganizationHeaderRenderer _headerRenderer = Substitute.For<IOrganizationHeaderRenderer>();
    private readonly IHeadlinePercentageRenderer _percentageRenderer = Substitute.For<IHeadlinePercentageRenderer>();

    private readonly CreateReportRequestCommandHandler _sut;

    public CreateReportRequestCommandHandlerTests()
    {
        _unitOfWork.SaveAsync().Returns(Task.CompletedTask);
        _dataFetcher
            .GetAsync(Arg.Any<OrganizationId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<DataPoint>(), Enumerable.Empty<DataPoint>(), Enumerable.Empty<DataPoint>()));
        _percentageProcessor.Calculate(Arg.Any<IReadOnlyList<HourlyEnergy>>()).Returns(100);
        _svgRenderer.Render(Arg.Any<IReadOnlyList<HourlyEnergy>>()).Returns(new EnergySvgResult("<svg></svg>"));
        _headerRenderer.Render(Arg.Any<string>(), Arg.Any<string>()).Returns("<header/>");
        _percentageRenderer.Render(Arg.Any<double>(), Arg.Any<string>()).Returns("<percent/>");

        var dummyPdf = new byte[] { 0x01, 0x02, 0x03 };
        var successResult = new GeneratePdfResult(
            IsSuccess: true,
            ErrorContent: null,
            PdfBytes: dummyPdf
        );
        _mediator
            .Send(Arg.Any<GeneratePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(successResult));

        _sut = new CreateReportRequestCommandHandler(
            _reports,
            _unitOfWork,
            _mediator,
            _logger,
            _dataFetcher,
            _percentageProcessor,
            _svgRenderer,
            _headerRenderer,
            _percentageRenderer
        );
    }

    [Fact]
    public async Task GivenValidRangeAndPdfSucceeds_WhenCallingHandler_ThenReportIsCompletedAndBytesPersisted()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();
        var cmd = new CreateReportRequestCommand(reportId, orgId, OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end);

        Report captured = null!;
        _reports
            .When(x => x.AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Id.Should().Be(reportId);
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.Content.Should().Equal(new byte[] { 0x01, 0x02, 0x03 });

        await _reports.Received(1)
            .AddAsync(Arg.Is<Report>(r => r.Id == reportId), Arg.Any<CancellationToken>());
        await _reports.Received(1)
            .UpdateAsync(Arg.Is<Report>(r => r.Id == reportId && r.Status == ReportStatus.Completed),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(2).SaveAsync();
    }

    [Fact]
    public async Task GivenValidRangeAndPdfFails_WhenCallingHandler_ThenReportIsFailed()
    {
        var errorMsg = "Rendering error";
        var failureResult = new GeneratePdfResult(
            IsSuccess: false,
            ErrorContent: errorMsg,
            PdfBytes: null
        );
        _mediator
            .Send(Arg.Any<GeneratePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(failureResult));

        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();
        var cmd = new CreateReportRequestCommand(reportId, orgId, OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end);

        Report captured = null!;
        _reports
            .When(x => x.AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Status.Should().Be(ReportStatus.Failed);
        captured.Content.Should().BeNull();

        await _reports.Received(1)
            .AddAsync(Arg.Is<Report>(r => r.Id == reportId), Arg.Any<CancellationToken>());
        await _reports.Received(1)
            .UpdateAsync(Arg.Is<Report>(r => r.Id == reportId && r.Status == ReportStatus.Failed),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(2).SaveAsync();
    }

    [Fact]
    public async Task GivenRangeExceedsOneYear_WhenCallingHandler_ThenThrowsBusinessException()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-400);
        var end = UnixTimestamp.Now();
        var cmd = new CreateReportRequestCommand(Guid.NewGuid(), orgId, OrganizationName.Create("Organization Name"), Tin.Create("13371337"), start, end);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(cmd, CancellationToken.None));

        await _reports.DidNotReceive().AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>());
        await _reports.DidNotReceive().UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveAsync();
    }
}
