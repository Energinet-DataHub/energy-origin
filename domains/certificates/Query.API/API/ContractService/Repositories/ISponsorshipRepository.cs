using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.ContractService.Repositories;

public interface ISponsorshipRepository
{
    Task<DateTimeOffset?> GetEndDateAsync(Gsrn gsrn, CancellationToken token = default);
}

public class SponsorshipRepository : ISponsorshipRepository
{
    private readonly ApplicationDbContext _db;
    public SponsorshipRepository(ApplicationDbContext db) => _db = db;

    public async Task<DateTimeOffset?> GetEndDateAsync(
        Gsrn gsrn,
        CancellationToken token = default)
    {
        return await _db.Sponsorships
            .AsNoTracking()
            .Where(s => s.SponsorshipGSRN == gsrn)
            .Select(s => (DateTimeOffset?)s.SponsorshipEndDate)
            .FirstOrDefaultAsync(token);
    }
}
