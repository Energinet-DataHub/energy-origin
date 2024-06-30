using API.Models;
using API.Repository;

namespace API.UnitTests.Repository;

public class FakeAffiliationRepository : IAffiliationRepository
{
    private readonly List<Affiliation> _affiliations = new List<Affiliation>();

    public Task<Affiliation> GetAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
    {
        var affiliation = _affiliations.FirstOrDefault(a => a.UserId == userId && a.OrganizationId == organizationId);
        if (affiliation == null)
        {
            throw new InvalidOperationException("Affiliation not found");
        }

        return Task.FromResult(affiliation);
    }

    public Task AddAsync(Affiliation entity, CancellationToken cancellationToken)
    {
        if (_affiliations.Any(a => a.UserId == entity.UserId && a.OrganizationId == entity.OrganizationId))
        {
            throw new InvalidOperationException("Affiliation with same UserId and OrganizationId already added");
        }

        _affiliations.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Affiliation entity)
    {
        _affiliations.Remove(entity);
    }

    public void Update(Affiliation entity)
    {
        var existingAffiliation = _affiliations.FirstOrDefault(a => a.UserId == entity.UserId && a.OrganizationId == entity.OrganizationId);
        if (existingAffiliation == null)
        {
            throw new InvalidOperationException("Affiliation not found");
        }

        _affiliations.Remove(existingAffiliation);
        _affiliations.Add(entity);
    }

    public IQueryable<Affiliation> Query()
    {
        return _affiliations.AsQueryable();
    }
}
