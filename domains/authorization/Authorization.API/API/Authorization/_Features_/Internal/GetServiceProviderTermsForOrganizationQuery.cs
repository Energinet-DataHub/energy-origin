using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
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
            throw new EntityNotFoundException(usersAffiliatedOrganizationsId, typeof(Organization));
        }

        return organization.ServiceProviderTermsAccepted;
    }
}
