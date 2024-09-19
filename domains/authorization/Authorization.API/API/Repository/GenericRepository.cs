using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context = context ?? throw new ArgumentNullException(nameof(context));

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
        Context.Set<T>().Update(entity);
    }

    public IQueryable<T> Query()
    {
        return Context.Set<T>().AsQueryable();
    }
}
