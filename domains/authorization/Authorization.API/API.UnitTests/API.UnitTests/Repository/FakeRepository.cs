using API.Models;
using API.Repository;
using EnergyOrigin.Setup.Exceptions;
using MockQueryable;

namespace API.UnitTests.Repository;

public class FakeGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class, IEntity<Guid>
{
    private readonly List<TEntity> _entities = new List<TEntity>();

    public Task<TEntity> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = _entities.FirstOrDefault(e => e.Id == id);
        if (entity is null)
        {
            throw new EntityNotFoundException(id, typeof(TEntity));
        }

        return Task.FromResult(entity);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_entities.Any(e => e.Id == entity.Id))
        {
            throw new InvalidOperationException("Entity with same id already added");
        }

        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(TEntity entity)
    {
        var removed = _entities.Remove(entity);
        if (!removed)
        {
            throw new InvalidOperationException("Entity not found");
        }
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            Remove(entity);
        }
    }

    public void Update(TEntity entity)
    {
        var existingEntity = _entities.FirstOrDefault(e => e.Id == entity.Id);
        if (existingEntity is null)
        {
            throw new EntityNotFoundException(entity.Id.ToString(), typeof(TEntity).Name);
        }

        _entities.Remove(existingEntity);
        _entities.Add(entity);
    }

    public IQueryable<TEntity> Query()
    {
        return _entities.BuildMock().AsQueryable();
    }
}

public class FakeClientRepository : FakeGenericRepository<Client>, IClientRepository
{
    public Task<bool> ExternalClientHasAccessThroughOrganization(Guid clientId, Guid organizationId)
    {
        return Task.FromResult(true);
    }
}

public class FakeOrganizationRepository : FakeGenericRepository<Organization>, IOrganizationRepository;

public class FakeUserRepository : FakeGenericRepository<User>, IUserRepository;

public class FakeOrganizationConsentRepository : FakeGenericRepository<OrganizationConsent>, IOrganizationConsentRepository;

public class FakeTermsRepository : FakeGenericRepository<Terms>, ITermsRepository;

public class FakeWhitelistedRepository : FakeGenericRepository<Whitelisted>, IWhitelistedRepository;
