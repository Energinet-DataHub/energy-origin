using API.Authorization.Exceptions;
using API.Models;
using API.Repository;
using MockQueryable.NSubstitute;

namespace API.UnitTests.Repository;

public class FakeAffiliationRepository : IAffiliationRepository
{
    private readonly List<Affiliation> _entities = [];

    public Task<Affiliation> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Affiliation entity, CancellationToken cancellationToken)
    {
        if (_entities.Exists(e => e.OrganizationId == entity.OrganizationId && e.UserId == entity.UserId))
        {
            throw new InvalidOperationException("Entity with same id already added");
        }
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Affiliation entity)
    {
        var removed = _entities.Remove(entity);
        if (!removed)
        {
            throw new InvalidOperationException("Entity not found");
        }
    }

    public void Update(Affiliation entity)
    {
        var existingEntity = _entities.Find(e => e.OrganizationId == entity.OrganizationId && e.UserId == entity.UserId);
        if (existingEntity is null)
        {
            throw new EntityNotFoundException(entity.OrganizationId.ToString() + entity.UserId, nameof(Affiliation));
        }
        _entities.Remove(existingEntity);
        _entities.Add(entity);
    }

    public IQueryable<Affiliation> Query()
    {
        return _entities.BuildMock().AsQueryable();
    }
}
