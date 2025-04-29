using System;
using System.Threading.Tasks;
using API.Models;
using API.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Repository;

public interface IClientRepository : IGenericRepository<Client>
{
    Task<bool> ExternalClientHasAccessThroughOrganization(Guid clientId, Guid organizationId);
}

public class ClientRepository(ApplicationDbContext context) : GenericRepository<Client>(context), IClientRepository
{
    public async Task<bool> ExternalClientHasAccessThroughOrganization(Guid clientId, Guid organizationId)
    {
        var hasAccess = await Context.Clients.AnyAsync(c =>
            c.IdpClientId == IdpClientId.Create(clientId) && c.OrganizationId == organizationId &&
            c.ClientType == ClientType.External);

        return hasAccess;
    }
}
