using System;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext Context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<T> GetAsync(Guid id)
    {
        return await Context.Set<T>().FindAsync(id) ?? throw new EntityNotFoundException(id, typeof(T).Name);
    }

    public async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Context.Set<T>().AddAsync(entity);
    }

    public Task RemoveAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Set<T>().Remove(entity);
        return Task.CompletedTask;

    }

    public Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
