using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record GetServiceProviderTermsForOrganizationQuery(OrganizationId OrgId) : IRequest<bool>;

public class GetServiceProviderTermsQueryHandler(
    IOrganizationRepository organizationRepository
) : IRequestHandler<GetServiceProviderTermsForOrganizationQuery, bool>
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

        return organization.ServiceProviderTermsAccepted;
    }
}
