using System;
using API.Models;
using API.Repository;

namespace API.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Users = new UserRepository(_context);
        Organizations = new OrganizationRepository(_context);
        Affiliations = new AffiliationRepository(_context);
        Consents = new ConsentRepository(_context);
        Clients = new ClientRepository(_context);
    }

    public IUserRepository Users { get; }
    public IOrganizationRepository Organizations { get; }
    public IAffiliationRepository Affiliations { get; }
    public IConsentRepository Consents { get; }
    public IClientRepository Clients { get; }

    public int Complete()
    {
        return _context.SaveChanges();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
