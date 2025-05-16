using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Controllers;
using API.Transfer.Api.Repository;
using MediatR;

namespace API.Transfer.Api._Features_;

public class GetReportStatusesQueryHandler(IReportRepository reports)
    : IRequestHandler<GetReportStatusesQuery, IEnumerable<ReportStatusApiResponse>>
{
    public async Task<IEnumerable<ReportStatusApiResponse>> Handle(
        GetReportStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var all = await reports.GetAllAsync(cancellationToken);
        return all
            .Select(report => new ReportStatusApiResponse(
                report.Id,
                report.CreatedAt.EpochSeconds,
                report.Status)
            );
    }
}

public record GetReportStatusesQuery() : IRequest<IEnumerable<ReportStatusApiResponse>>;
