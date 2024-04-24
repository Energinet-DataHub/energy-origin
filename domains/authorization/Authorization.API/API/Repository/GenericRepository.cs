using System;
using API.Models;

namespace API.Repository;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public T Get(Guid id)
    {
        return _context.Set<T>().Find(id) ?? throw new EntityNotFoundException(id, nameof(T));
    }

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Add(entity);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Remove(entity);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Set<T>().Update(entity);
    }
}
