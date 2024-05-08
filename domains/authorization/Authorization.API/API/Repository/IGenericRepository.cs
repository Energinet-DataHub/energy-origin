using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Repository;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(object[] keys, CancellationToken cancellationToken);
    Task<TEntity> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
    void Remove(TEntity entity);
    void Update(TEntity entity);
    IQueryable<TEntity> Query();
}
