using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record GetServiceProviderTermsForOrganizationQuery(OrganizationId OrgId) : IRequest<bool>;


public class GetServiceProviderTermsQueryHandler(IOrganizationRepository organizationRepository, IServiceProviderTermsRepository serviceProviderTermsRepository)
    : IRequestHandler<GetServiceProviderTermsForOrganizationQuery, bool>
{
    public async Task<bool> Handle(GetServiceProviderTermsForOrganizationQuery request, CancellationToken cancellationToken)
    {
        var usersAffiliatedOrganizationsId = request.OrgId.Value;

        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Id == usersAffiliatedOrganizationsId, cancellationToken);

        if (organization == null)
        {
            throw new EntityNotFoundException(nameof(organization), "Organization does not exist.");
        }

        var latestServiceProviderTerms = await serviceProviderTermsRepository.Query()
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestServiceProviderTerms == null)
        {
            throw new EntityNotFoundException(nameof(latestServiceProviderTerms), "No Service Provider Terms configured.");
        }

        return organization.ServiceProviderTermsAccepted &&
               organization.ServiceProviderTermsVersion == latestServiceProviderTerms.Version;
    }
}


