using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;

namespace API.Repository;

public interface IAffiliationRepository
{
    Task<Affiliation> GetAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken);
    Task AddAsync(Affiliation entity, CancellationToken cancellationToken);
    void Remove(Affiliation entity);
    void Update(Affiliation entity);
    IQueryable<Affiliation> Query();
}

public class AffiliationRepository(ApplicationDbContext context) : IAffiliationRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Affiliation> GetAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
    {
        var entity = await _context.Affiliations.FindAsync(new object[] { userId, organizationId }, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException("Affiliation not found");
        }

        return entity;
    }

    public async Task AddAsync(Affiliation entity, CancellationToken cancellationToken)
    {
        if (_context.Affiliations.Any(a => a.UserId == entity.UserId && a.OrganizationId == entity.OrganizationId))
        {
            throw new InvalidOperationException("Affiliation with same UserId and OrganizationId already added");
        }

        await _context.Affiliations.AddAsync(entity, cancellationToken);
    }

    public void Remove(Affiliation entity)
    {
        _context.Affiliations.Remove(entity);
    }

    public void Update(Affiliation entity)
    {
        _context.Affiliations.Update(entity);
    }

    public IQueryable<Affiliation> Query()
    {
        return _context.Affiliations.AsQueryable();
    }
}
