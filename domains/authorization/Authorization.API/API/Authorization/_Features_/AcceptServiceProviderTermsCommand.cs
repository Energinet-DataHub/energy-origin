using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
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
            throw new EntityNotFoundException(usersAffiliatedOrganizationsId, typeof(Organization));
        }

        if (!usersAffiliatedOrganization.ServiceProviderTermsAccepted)
        {
            usersAffiliatedOrganization.AcceptServiceProviderTerms();
        }

        await unitOfWork.CommitAsync();
    }
}
