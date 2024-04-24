using System;
using API.Repository;

namespace API.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IOrganizationRepository Organizations { get; }
        IAffiliationRepository Affiliations { get; }
        IClientRepository Clients { get; }
        IConsentRepository Consents { get; }
        int Complete();
    }
}
