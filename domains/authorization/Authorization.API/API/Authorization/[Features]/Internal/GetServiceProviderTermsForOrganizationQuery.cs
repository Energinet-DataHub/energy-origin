using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record GetServiceProviderTermsForOrganizationQuery(string OrgCvr) : IRequest<bool>;


public class GetServiceProviderTermsQueryHandler(IOrganizationRepository organizationRepository, IServiceProviderTermsRepository serviceProviderTermsRepository)
    : IRequestHandler<GetServiceProviderTermsForOrganizationQuery, bool>
{
    public async Task<bool> Handle(GetServiceProviderTermsForOrganizationQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrgCvr))
        {
            throw new InvalidConfigurationException("Organization CVR is required.");
        }

        var organizationCvr = Tin.Create(request.OrgCvr);

        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == organizationCvr, cancellationToken);

        if (organization == null)
        {
            throw new InvalidConfigurationException("Organization not found.");
        }

        var latestServiceProviderTerms = await serviceProviderTermsRepository.Query()
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestServiceProviderTerms == null)
        {
            throw new InvalidConfigurationException("No Service Provider Terms configured.");
        }

        return organization.ServiceProviderTermsAccepted &&
               organization.ServiceProviderTermsVersion == latestServiceProviderTerms.Version;
    }
}


