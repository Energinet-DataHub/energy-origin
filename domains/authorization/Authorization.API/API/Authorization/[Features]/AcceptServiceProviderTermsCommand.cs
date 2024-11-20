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

public record AcceptServiceProviderTermsCommand(OrganizationId OrgId) : IRequest;

public class AcceptServiceProviderTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptServiceProviderTermsCommand>
{
    public async Task Handle(AcceptServiceProviderTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var usersAffiliatedOrganizationsId = request.OrgId.Value;

        var usersAffiliatedOrganization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Id == usersAffiliatedOrganizationsId, cancellationToken);

        if (usersAffiliatedOrganization == null)
        {
            throw new EntityNotFoundException(nameof(usersAffiliatedOrganization), "Organization does not exist.");
        }

        if (!usersAffiliatedOrganization.ServiceProviderTermsAccepted)
        {
            usersAffiliatedOrganization.AcceptServiceProviderTerms();
        }

        await unitOfWork.CommitAsync();
    }
}
