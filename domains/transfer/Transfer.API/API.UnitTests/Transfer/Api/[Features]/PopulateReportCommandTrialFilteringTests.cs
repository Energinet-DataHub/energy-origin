using System;
using System.Collections.Generic;
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
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class PopulateReportCommandTrialFilteringTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<PopulateReportCommandHandler> _logger = Substitute.For<ILogger<PopulateReportCommandHandler>>();
    private readonly IConsumptionService _consumptionService = Substitute.For<IConsumptionService>();
    private readonly IWalletClient _walletClient = Substitute.For<IWalletClient>();
    private readonly IEnergySvgRenderer _svgRenderer = Substitute.For<IEnergySvgRenderer>();
    private readonly IOrganizationHeaderRenderer _headerRenderer = Substitute.For<IOrganizationHeaderRenderer>();
    private readonly IHeadlinePercentageRenderer _percentageRenderer = Substitute.For<IHeadlinePercentageRenderer>();
    private readonly ILogoRenderer _logoRenderer = Substitute.For<ILogoRenderer>();
    private readonly IStyleRenderer _styleRenderer = Substitute.For<IStyleRenderer>();

    private readonly PopulateReportCommandHandler _sut;
    private readonly EnergyDataFetcher _realDataFetcher;

    public PopulateReportCommandTrialFilteringTests()
    {
        _unitOfWork.ReportRepository.Returns(_reports);
        _unitOfWork.SaveAsync().Returns(Task.CompletedTask);
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

        _realDataFetcher = new EnergyDataFetcher(_consumptionService, _walletClient);

        _sut = new PopulateReportCommandHandler(
            _unitOfWork,
            _mediator,
            _logger,
            _realDataFetcher,
            new EnergyDataFormatter(),
            new MunicipalityPercentageProcessor(),
            new CoverageProcessor(),
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
    public async Task Handle_WhenTrialOrganization_ThenOnlyTrialClaimsAreProcessed()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: "trial",
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        // Create mixed claims
        var allClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo
                {
                    Count = allClaims.Count,
                    Offset = 0,
                    Limit = allClaims.Count,
                    Total = allClaims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(true);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());

        await _consumptionService.Received(1)
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNonTrialOrganization_ThenOnlyNonTrialClaimsAreProcessed()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: "normal",
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        // Create mixed claims
        var allClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = allClaims,
                Metadata = new PageInfo
                {
                    Count = allClaims.Count,
                    Offset = 0,
                    Limit = allClaims.Count,
                    Total = allClaims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(false);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());

        await _consumptionService.Received(1)
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTrialOrganization_AndNoTrialClaimsExist_ThenReportIsCompletedWithEmptyData()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: "trial",
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var nonTrialClaims = new List<Claim>
        {
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: false)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = nonTrialClaims,
                Metadata = new PageInfo
                {
                    Count = nonTrialClaims.Count,
                    Offset = 0,
                    Limit = nonTrialClaims.Count,
                    Total = nonTrialClaims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(true);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNonTrialOrganization_AndNoNonTrialClaimsExist_ThenReportIsCompletedWithEmptyData()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: "normal",
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        // Only trial claims
        var trialClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: true)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = trialClaims,
                Metadata = new PageInfo
                {
                    Count = trialClaims.Count,
                    Offset = 0,
                    Limit = trialClaims.Count,
                    Total = trialClaims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(false);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("trial", true)]
    [InlineData("normal", false)]
    public async Task Handle_WhenOrganizationStatusIsSet_ThenCorrectTrialParameterIsPassed(string orgStatus, bool expectedIsTrial)
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: orgStatus,
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var claims = new List<Claim>
        {
            CreateClaim(isTrial: expectedIsTrial)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = claims,
                Metadata = new PageInfo
                {
                    Count = claims.Count,
                    Offset = 0,
                    Limit = claims.Count,
                    Total = claims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(expectedIsTrial);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMixedClaimsExist_ThenFilteringIsAppliedCorrectly()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var start = UnixTimestamp.Now().AddDays(-7);
        var end = UnixTimestamp.Now();
        var reportId = Guid.NewGuid();

        var report = Report.Create(
            reportId,
            orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            orgStatus: "trial",
            start,
            end
        );

        var consumptionHours = new List<ConsumptionHour>
        {
            new(10) { KwhQuantity = 100 }
        };

        var mixedClaims = new List<Claim>
        {
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false),
            CreateClaim(isTrial: true),
            CreateClaim(isTrial: false)
        };

        _reports.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(report);

        _consumptionService
            .GetTotalAndAverageHourlyConsumption(orgId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((consumptionHours, consumptionHours));

        _walletClient
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>())
            .Returns(new ResultList<Claim>
            {
                Result = mixedClaims,
                Metadata = new PageInfo
                {
                    Count = mixedClaims.Count,
                    Offset = 0,
                    Limit = mixedClaims.Count,
                    Total = mixedClaims.Count
                }
            });

        var cmd = new PopulateReportCommand(reportId);

        Report captured = null!;
        _reports
            .When(x => x.UpdateAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<Report>());

        await _sut.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured.Status.Should().Be(ReportStatus.Completed);
        captured.IsTrial.Should().Be(true);

        await _walletClient.Received(1)
            .GetClaimsAsync(orgId.Value, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), TimeMatch.All, Arg.Any<CancellationToken>());
    }

    private static Claim CreateClaim(bool isTrial)
    {
        return new Claim
        {
            ClaimId = Guid.NewGuid(),
            Quantity = 100,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProductionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrial.ToString() }
                }
            },
            ConsumptionCertificate = new ClaimedCertificate
            {
                FederatedStreamId = new FederatedStreamId { Registry = "test", StreamId = Guid.NewGuid() },
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                End = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                GridArea = "DK1",
                Attributes = new Dictionary<string, string>
                {
                    { "IsTrial", isTrial.ToString() }
                }
            }
        };
    }
}
