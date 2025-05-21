using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace API.Transfer.Api._Features_;

public class GetReportStatusesQueryHandler(IReportRepository reports)
    : IRequestHandler<GetReportStatusesQuery, GetReportStatusesQueryResult>
{
    public async Task<GetReportStatusesQueryResult> Handle(
        GetReportStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var reports1 = await reports.GetByOrganizationAsync(
            request.OrganizationId,
            cancellationToken);

        var items = reports1
            .Select(r => new ReportStatusItem(
                r.Id,
                r.CreatedAt.EpochSeconds,
                r.Status))
            .ToList();

        return new GetReportStatusesQueryResult(items);
    }
}

public record GetReportStatusesQuery(OrganizationId OrganizationId) : IRequest<GetReportStatusesQueryResult>;
public record ReportStatusItem(Guid Id, long CreatedAt, ReportStatus Status);
public record GetReportStatusesQueryResult(List<ReportStatusItem> Result);
