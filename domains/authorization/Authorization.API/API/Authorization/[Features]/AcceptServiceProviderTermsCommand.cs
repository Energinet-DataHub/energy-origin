using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record AcceptServiceProviderTermsCommand(string OrgCvr) : IRequest;

public class AcceptServiceProviderTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    IServiceProviderTermsRepository serviceProviderTermsRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptServiceProviderTermsCommand>
{
    public async Task Handle(AcceptServiceProviderTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var usersOrganizationsCvr = Tin.Create(request.OrgCvr);

        var usersAffiliatedOrganization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == usersOrganizationsCvr, cancellationToken);

        if (usersAffiliatedOrganization == null)
        {
            throw new InvalidConfigurationException("User not Affiliated with any Organization");
        }

        var latestServiceProviderTerms = await serviceProviderTermsRepository.Query()
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestServiceProviderTerms == null)
        {
            throw new InvalidConfigurationException("No Service Provider Terms configured");
        }

        if (!usersAffiliatedOrganization.ServiceProviderTermsAccepted || usersAffiliatedOrganization.ServiceProviderTermsVersion != latestServiceProviderTerms.Version)
        {
            usersAffiliatedOrganization.AcceptServiceProviderTerms(latestServiceProviderTerms);
        }

        await unitOfWork.CommitAsync();
    }
}
