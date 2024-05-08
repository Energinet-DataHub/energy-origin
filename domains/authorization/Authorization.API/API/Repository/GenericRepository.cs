using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<T> GetAsync(object[] keys, CancellationToken cancellationToken)
    {
        var key = string.Join(" ", keys.Select(k => k.ToString()));

        return await Context.Set<T>().FindAsync(keys, cancellationToken) ??
               throw new EntityNotFoundException(key, typeof(T).Name);
    }

    public async Task<T> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Context.Set<T>().FindAsync(id, cancellationToken) ??
               throw new EntityNotFoundException(id.ToString(), typeof(T).Name);
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

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Entry(entity).State = EntityState.Modified;
    }

    public IQueryable<T> Query()
    {
        return Context.Set<T>().AsQueryable();
    }
}
