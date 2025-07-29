using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.ContractService.Clients;

public interface IOrganizationClient
{
    Task<AdminPortalOrganizationResponse?> GetOrganization(Guid organizationId, CancellationToken cancellationToken);
}
