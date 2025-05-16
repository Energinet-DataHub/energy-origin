using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken);
    Task<Report?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken);
    Task UpdateAsync(Report report, CancellationToken cancellationToken);
    Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken);
    Task<IEnumerable<Report>> GetByOrganizationAsync(OrganizationId organizationId, CancellationToken cancellationToken);
}

public class ReportRepository(ApplicationDbContext context) : IReportRepository
{
    public async Task AddAsync(Report report, CancellationToken cancellationToken)
        => await context.Reports.AddAsync(report, cancellationToken);

    public async Task<Report?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken)
        => await context.Reports.FindAsync(new object[] { reportId }, cancellationToken);

    public async Task UpdateAsync(Report report, CancellationToken cancellationToken)
    {
        context.Reports.Update(report);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Report>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Reports.ToListAsync(cancellationToken);

    public async Task<IEnumerable<Report>> GetByOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        return await context.Reports
            .Where(r => r.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
