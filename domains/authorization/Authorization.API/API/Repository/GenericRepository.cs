using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using EnergyOrigin.Setup.Exceptions;

namespace API.Repository;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<T> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Set<T>().FindAsync(id, cancellationToken) ??
               throw new EntityNotFoundException(id, typeof(T));
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Context.Set<T>().AddAsync(entity, cancellationToken);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Context.Set<T>().RemoveRange(entities);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Update(entity);
    }

    public IQueryable<T> Query()
    {
        return Context.Set<T>().AsQueryable();
    }
}
