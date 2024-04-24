using System;
using System.Collections.Generic;

namespace API.Repository;

public interface IGenericRepository<TEntity> where TEntity : class
{
    TEntity Get(Guid id);
    void Add(TEntity entity);
    void Remove(TEntity entity);
    void Update(TEntity entity);
}
