using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Repository;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Transfer.Api.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version1)]
[Route("api/reports")]
public class ReportsController(
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policy.FrontendOr3rdParty)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Initiates asynchronous report generation.")]
    public async Task<IActionResult> StartReportGeneration(
        [FromBody] StartReportRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.EndDate - request.StartDate).TotalDays > 365)
            return BadRequest("Date range cannot exceed 1 year.");

        var report = new Report
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = ReportStatus.Pending,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.SaveAsync();

        await publishEndpoint.Publish(
            new GenerateReportCommand(report.Id, request.StartDate, request.EndDate),
            cancellationToken);

        return AcceptedAtAction(nameof(GetReportStatus), new { reportId = report.Id }, null);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Lists all generated reports with metadata.")]
    public async Task<IActionResult> ListReports(CancellationToken cancellationToken)
    {
        var reports = await reportRepository.GetAllAsync(cancellationToken);
        return Ok(reports.Select(r => new ReportDto(r.Id, r.CreatedAt, r.Status)));
    }

    [HttpGet("{reportId}")]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Gets the status of a specific report.")]
    public async Task<IActionResult> GetReportStatus(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        return report == null ? NotFound() : Ok(new ReportDto(report.Id, report.CreatedAt, report.Status));
    }
}

public record StartReportRequest(DateTimeOffset StartDate, DateTimeOffset EndDate);
public record ReportDto(Guid Id, DateTimeOffset CreatedAt, ReportStatus Status);
