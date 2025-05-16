using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using MassTransit;
using MediatR;
using EnergyOrigin.Domain.ValueObjects;
using DataContext.Models;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using EnergyOrigin.Setup.Exceptions;

namespace API.Transfer.Api._Features_;

public class CreateReportRequestCommandHandler(
    IReportRepository reports,
    IUnitOfWork unitOfWork,
    IPublishEndpoint bus)
    : IRequestHandler<CreateReportRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreateReportRequestCommand request, CancellationToken cancellationToken)
    {
        var rangeSeconds = request.EndDate.EpochSeconds - request.StartDate.EpochSeconds;
        if (rangeSeconds > UnixTimestamp.SecondsPerDay * 365)
            throw new BusinessException("Date range cannot exceed 1 year.");

        var report = new Report
        {
            Id = Guid.NewGuid(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = UnixTimestamp.Now(),
            Status = ReportStatus.Pending
        };
        await reports.AddAsync(report, cancellationToken);

        await bus.Publish(
            new ReportRequestCreated(
                report.Id,
                request.StartDate.EpochSeconds,
                request.EndDate.EpochSeconds),
            cancellationToken);

        await unitOfWork.SaveAsync();

        return report.Id;
    }
}

public record CreateReportRequestCommand(
    UnixTimestamp StartDate,
    UnixTimestamp EndDate
) : IRequest<Guid>;
