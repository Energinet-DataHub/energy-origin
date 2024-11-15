using API.Authorization.Exceptions;
using API.Models;
using API.Repository;
using MockQueryable.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace API.UnitTests.Repository;

public class FakeGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class, IEntity<Guid>
{
    private readonly List<TEntity> _entities = new List<TEntity>();

    public Task<TEntity> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = _entities.FirstOrDefault(e => e.Id == id);
        if (entity is null)
        {
            throw new EntityNotFoundException(id.ToString(), typeof(TEntity).Name);
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

public class FakeClientRepository : FakeGenericRepository<Client>, IClientRepository;

public class FakeOrganizationRepository : FakeGenericRepository<Organization>, IOrganizationRepository;

public class FakeUserRepository : FakeGenericRepository<User>, IUserRepository;

public class FakeOrganizationConsentRepository : FakeGenericRepository<OrganizationConsent>, IOrganizationConsentRepository;

public class FakeTermsRepository : FakeGenericRepository<Terms>, ITermsRepository;
public class FakeServiceProviderTermsRepository : FakeGenericRepository<ServiceProviderTerms>, IServiceProviderTermsRepository;
