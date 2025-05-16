using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Pdf.V1;
using EnergyOrigin.Setup.Exceptions;
using MassTransit;
using MediatR;

namespace API.Transfer.Api._Features_;

public class CreateReportRequestCommandHandler
    : IRequestHandler<CreateReportRequestCommand, Guid>
{
    private readonly IReportRepository _reports;
    private readonly IUnitOfWork       _unitOfWork;
    private readonly IPublishEndpoint  _bus;

    public CreateReportRequestCommandHandler(
        IReportRepository reports,
        IUnitOfWork       unitOfWork,
        IPublishEndpoint  bus)
    {
        _reports    = reports;
        _unitOfWork = unitOfWork;
        _bus        = bus;
    }

    public async Task<Guid> Handle(
        CreateReportRequestCommand request,
        CancellationToken         cancellationToken)
    {
        var duration = request.EndDate.EpochSeconds - request.StartDate.EpochSeconds;
        if (duration > UnixTimestamp.SecondsPerDay * 365)
            throw new BusinessException("Date range cannot exceed 1 year.");

        var report = Report.Create(
            request.OrganizationId,
            request.StartDate,
            request.EndDate);

        await _reports.AddAsync(report, cancellationToken);

        await _bus.Publish(
            new ReportRequestCreated(
                report.Id,
                request.StartDate.EpochSeconds,
                request.EndDate.EpochSeconds),
            cancellationToken);

        await _unitOfWork.SaveAsync();

        return report.Id;
    }
}

public record CreateReportRequestCommand(
    OrganizationId OrganizationId,
    UnixTimestamp  StartDate,
    UnixTimestamp  EndDate
) : IRequest<Guid>;
