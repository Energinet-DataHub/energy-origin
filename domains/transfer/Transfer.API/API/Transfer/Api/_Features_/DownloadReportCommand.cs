using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using DataContext.Models;
using MediatR;

namespace API.Transfer.Api._Features_;

public record DownloadReportCommand(Guid ReportId, Guid OrganizationId) : IRequest<DownloadReportResponse>;

public record DownloadReportResponse(byte[]? Content);

public class DownloadReportCommandHandler : IRequestHandler<DownloadReportCommand, DownloadReportResponse?>
{
    private readonly IReportRepository _reportRepository;

    public DownloadReportCommandHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<DownloadReportResponse?> Handle(DownloadReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is not { Status: ReportStatus.Completed })
        {
            return null;
        }

        return new DownloadReportResponse(
            Content: report.Content
        );
    }
}
