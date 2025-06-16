using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class GetReportStatusesQueryHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly GetReportStatusesQueryHandler _sut;

    public GetReportStatusesQueryHandlerTests()
    {
        _sut = new GetReportStatusesQueryHandler(_reports);
    }

    [Fact]
    public async Task GivenReportsExist_WhenCallingHandler_ThenReturnsStatusItemsForMatchingOrganization()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());

        var report1 = Report.Create(
            id: Guid.NewGuid(),
            organizationId: orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            startDate: UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-7)),
            endDate: UnixTimestamp.Create(DateTimeOffset.UtcNow));

        var report2 = Report.Create(
            id: Guid.NewGuid(),
            organizationId: orgId,
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            startDate: UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-60)),
            endDate: UnixTimestamp.Create(DateTimeOffset.UtcNow));

        Report.Create(
            id: Guid.NewGuid(),
            organizationId: OrganizationId.Create(Guid.NewGuid()),
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            startDate: UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-180)),
            endDate: UnixTimestamp.Create(DateTimeOffset.UtcNow));

        Report.Create(
            id: Guid.NewGuid(),
            organizationId: OrganizationId.Create(Guid.NewGuid()),
            Any.OrganizationName(),
            EnergyTrackAndTrace.Testing.Any.Tin(),
            startDate: UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-365)),
            endDate: UnixTimestamp.Create(DateTimeOffset.UtcNow));

        _reports
            .GetByOrganizationAsync(orgId, Arg.Any<CancellationToken>())
            .Returns(new[] { report1, report2 });

        var query = new GetReportStatusesQuery(orgId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Result.Should().HaveCount(2);

        result.Result.Should().BeEquivalentTo(new[]
        {
            new ReportStatusItem(report1.Id, report1.CreatedAt.EpochSeconds, report1.Status),
            new ReportStatusItem(report2.Id, report2.CreatedAt.EpochSeconds, report2.Status)
        });
    }

    [Fact]
    public async Task GivenNoReportsExist_WhenCallingHandler_ThenReturnsEmptyList()
    {
        var orgId = OrganizationId.Create(Guid.NewGuid());

        _reports
            .GetByOrganizationAsync(orgId, Arg.Any<CancellationToken>())
            .Returns([]);

        var query = new GetReportStatusesQuery(orgId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Result.Should().BeEmpty();
    }
}
