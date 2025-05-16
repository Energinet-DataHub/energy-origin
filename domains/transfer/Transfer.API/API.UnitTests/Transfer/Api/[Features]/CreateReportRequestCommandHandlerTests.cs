using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using EnergyOrigin.Setup.Exceptions;
using FluentAssertions;
using MassTransit;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api._Features_;

public class CreateReportRequestCommandHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublishEndpoint _bus = Substitute.For<IPublishEndpoint>();
    private readonly CreateReportRequestCommandHandler _sut;

    public CreateReportRequestCommandHandlerTests()
    {
        _unitOfWork
            .SaveAsync()
            .Returns(Task.CompletedTask);
        _bus
            .Publish(Arg.Any<ReportRequestCreated>(), TestContext.Current.CancellationToken)
            .Returns(Task.CompletedTask);

        _sut = new CreateReportRequestCommandHandler(_reports, _unitOfWork, _bus);
    }

    [Fact]
    public async Task Handle_ValidRange_PersistsReportAndPublishesEvent()
    {
        var start = UnixTimestamp.Now().AddDays(-30);
        var end = UnixTimestamp.Now();
        var cmd = new CreateReportRequestCommand(start, end);

        Report? capturedReport = null;
        _reports
            .When(r => r.AddAsync(Arg.Any<Report>(), TestContext.Current.CancellationToken))
            .Do(ci => capturedReport = ci.Arg<Report>());

        var resultId = await _sut.Handle(cmd, TestContext.Current.CancellationToken);

        capturedReport.Should().NotBeNull();
        Assert.Equal(resultId, capturedReport!.Id);
        Assert.Equal(start, capturedReport.StartDate);
        Assert.Equal(end, capturedReport.EndDate);
        Assert.Equal(ReportStatus.Pending, capturedReport.Status);
        await _reports.Received(1).AddAsync(Arg.Any<Report>(), TestContext.Current.CancellationToken);

        await _bus.Received(1).Publish(
            Arg.Is<ReportRequestCreated>(e =>
                e.ReportId == resultId &&
                e.StartDate == start.EpochSeconds &&
                e.EndDate == end.EpochSeconds),
            TestContext.Current.CancellationToken);

        await _unitOfWork.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Handle_RangeExceedsOneYear_ThrowsBusinessException_NoSideEffects()
    {
        var start = UnixTimestamp.Now().AddDays(-366);
        var end = UnixTimestamp.Now();
        var cmd = new CreateReportRequestCommand(start, end);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.Handle(cmd, TestContext.Current.CancellationToken));

        await _reports.DidNotReceive().AddAsync(Arg.Any<Report>(), TestContext.Current.CancellationToken);
        await _bus.DidNotReceive().Publish(Arg.Any<ReportRequestCreated>(), TestContext.Current.CancellationToken);
        await _unitOfWork.DidNotReceive().SaveAsync();
    }
}
