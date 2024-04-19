using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;

namespace API.Data;

public class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : class
{
    protected readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public T? Get(Guid id)
    {
        return _context.Set<T>().Find(id);
    }

    public IEnumerable<T> GetAll()
    {
        return _context.Set<T>().ToList();
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
