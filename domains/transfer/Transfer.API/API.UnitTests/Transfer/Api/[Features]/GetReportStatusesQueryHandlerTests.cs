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

        var report1 = Report.Create(orgId,
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-2)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-1)));

        var report2 = Report.Create(orgId,
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-5)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-4)));

        Report.Create(
            OrganizationId.Create(Guid.NewGuid()),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-10)),
            UnixTimestamp.Create(DateTimeOffset.UtcNow.AddDays(-9)));

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
