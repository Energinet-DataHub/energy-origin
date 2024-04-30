using System;
using System.Threading.Tasks;

namespace API.Repository;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(Guid id);
    Task AddAsync(TEntity entity);
    Task RemoveAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
}
