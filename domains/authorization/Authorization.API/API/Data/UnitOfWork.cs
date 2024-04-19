using System;
using API.Models;

namespace API.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Users = new Repository<User>(_context);
        Organizations = new Repository<Organization>(_context);
        Affiliations = new Repository<Affiliation>(_context);
        Clients = new Repository<Client>(_context);
        Consents = new Repository<Consent>(_context);
    }

    public IRepository<User> Users { get; }
    public IRepository<Organization> Organizations { get; }
    public IRepository<Affiliation> Affiliations { get; }
    public IRepository<Client> Clients { get; }
    public IRepository<Consent> Consents { get; }

    public int Complete()
    {
        return _context.SaveChanges();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

