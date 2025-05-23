using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace API.IntegrationTests.Transfer.Api._Features_;

public class DownloadReportCommandTests
{
    [Fact]
    public async Task Handle_ReportIsCompleted_ReturnsContent()
    {
        var reportId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var report = Report.Create(reportId, orgId, UnixTimestamp.Now().AddDays(-14), UnixTimestamp.Now().AddDays(-7));
        report.MarkCompleted(content);
        var repo = Substitute.For<IReportRepository>();
        repo.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new DownloadReportCommandHandler(repo);
        var command = new DownloadReportCommand(reportId, orgId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(content, result!.Content);
    }

    [Fact]
    public async Task Handle_ReportIsNotCompleted_ReturnsNull()
    {
        var reportId = Guid.NewGuid();
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var report = Report.Create(reportId, orgId, UnixTimestamp.Now().AddDays(-14), UnixTimestamp.Now().AddDays(-7));
        report.MarkFailed();
        var repo = Substitute.For<IReportRepository>();
        repo.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);

        var handler = new DownloadReportCommandHandler(repo);
        var command = new DownloadReportCommand(reportId, orgId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result);
    }
}
