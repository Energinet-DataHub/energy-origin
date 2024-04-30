using System;
using System.Threading.Tasks;

namespace API.Repository;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(Guid id);
    Task AddAsync(TEntity entity);
    void Remove(TEntity entity);
    void Update(TEntity entity);
}
