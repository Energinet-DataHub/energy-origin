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
using API.Transfer.Api.Services;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class PopulateReportCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<PopulateReportCommandHandler> _logger = Substitute.For<ILogger<PopulateReportCommandHandler>>();
    private readonly IEnergyDataFetcher _dataFetcher = Substitute.For<IEnergyDataFetcher>();
    private readonly IEnergySvgRenderer _svgRenderer = Substitute.For<IEnergySvgRenderer>();
    private readonly IOrganizationHeaderRenderer _headerRenderer = Substitute.For<IOrganizationHeaderRenderer>();
    private readonly IHeadlinePercentageRenderer _percentageRenderer = Substitute.For<IHeadlinePercentageRenderer>();
    private readonly ILogoRenderer _logoRenderer = Substitute.For<ILogoRenderer>();
    private readonly IStyleRenderer _styleRenderer = Substitute.For<IStyleRenderer>();

    private readonly PopulateReportCommandHandler _sut;

    public PopulateReportCommandHandlerTests()
    {
        _unitOfWork.ReportRepository.Returns(_reports);
        _unitOfWork.SaveAsync().Returns(Task.CompletedTask);
        _dataFetcher
            .GetAsync(Arg.Any<OrganizationId>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<ConsumptionHour>().ToList(), Enumerable.Empty<Claim>().ToList()));
        _svgRenderer.Render(Arg.Any<IReadOnlyList<HourlyEnergy>>()).Returns(new EnergySvgResult("<svg></svg>"));
        _headerRenderer.Render(Arg.Any<string>(), Arg.Any<string>()).Returns("<header/>");
        _percentageRenderer.Render(Arg.Any<double>(), Arg.Any<string>()).Returns("<percent/>");
        _logoRenderer.Render().Returns("<svg></svg>");
        _styleRenderer.Render().Returns("<style></style>");

        var dummyPdf = new byte[] { 0x01, 0x02, 0x03 };
        var successResult = new GeneratePdfResult(
            IsSuccess: true,
            ErrorContent: null,
            PdfBytes: dummyPdf
        );
        _mediator
            .Send(Arg.Any<GeneratePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(successResult));

        _sut = new PopulateReportCommandHandler(
            _unitOfWork,
            _mediator,
            _logger,
            _dataFetcher,
            new EnergyDataFormatter(),
            new MunicipalityPercentageProcessor(),
            new CoverageProcessor(Substitute.For<ILogger<CoverageProcessor>>()),
            _svgRenderer,
            _headerRenderer,
            _percentageRenderer,
            new MunicipalityPercentageRenderer(),
            new OtherCoverageRenderer(),
            _logoRenderer,
            _styleRenderer
        );
    }

    [Fact]
    public async Task GivenValidRangeAndPdfSucceeds_WhenCallingHandler_ThenReportIsCompletedAndBytesPersisted()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(reportId, orgId, Any.OrganizationName(), EnergyTrackAndTrace.Testing.Any.Tin(), start, end);
        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        var cmd = new PopulateReportCommand(reportId);

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
            .UpdateAsync(Arg.Is<Report>(r => r.Id == reportId && r.Status == ReportStatus.Completed),
                Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveAsync();
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
        var cmd = new PopulateReportCommand(reportId);

        var report = Report.Create(reportId, orgId, Any.OrganizationName(), EnergyTrackAndTrace.Testing.Any.Tin(), start, end);
        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

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
            .UpdateAsync(Arg.Is<Report>(r => r.Id == reportId && r.Status == ReportStatus.Failed),
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveAsync();
    }
}
