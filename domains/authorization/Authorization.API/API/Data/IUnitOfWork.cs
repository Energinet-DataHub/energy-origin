using System;
using API.Models;

namespace API.Data;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Organization> Organizations { get; }
    IRepository<Affiliation> Affiliations { get; }
    IRepository<Client> Clients { get; }
    IRepository<Consent> Consents { get; }
    int Complete();
}

