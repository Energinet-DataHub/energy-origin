using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GrantConsentToOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IOrganizationConsentRepository organizationConsentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentToOrganizationCommand>
{
    public async Task Handle(GrantConsentToOrganizationCommand toOrganizationCommand, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(toOrganizationCommand.UserId);
        var organizationTin = Tin.Create(toOrganizationCommand.OrganizationCvr);
        var consentReceiverOrganizationId = toOrganizationCommand.OrganizationId.Value;

        // Throw not found if organization id is unknown
        await organizationRepository.GetAsync(consentReceiverOrganizationId, cancellationToken);

        var affiliatedOrganization = await userRepository.Query()
            .Where(u => u.IdpUserId == idpUserId)
            .SelectMany(u => u.Affiliations)
            .Where(a => a.Organization.Tin == organizationTin)
            .Select(a => a.Organization)
            .FirstOrDefaultAsync(cancellationToken);

        if (affiliatedOrganization is null)
        {
            throw new UserNotAffiliatedWithOrganizationCommandException();
        }

        var existingConsent = await organizationRepository
            .Query()
            .Where(o => o.Id == affiliatedOrganization.Id && o.OrganizationGivenConsents.Any(c =>
                c.ConsentReceiverOrganizationId == consentReceiverOrganizationId)).AnyAsync(cancellationToken);

        if (!existingConsent)
        {
            var organizationConsent = OrganizationConsent.Create(affiliatedOrganization.Id, consentReceiverOrganizationId, DateTimeOffset.UtcNow);
            await organizationConsentRepository.AddAsync(organizationConsent, cancellationToken);
        }
        else
        {
            throw new EntityAlreadyExistsException(nameof(OrganizationConsent));
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentToOrganizationCommand(Guid UserId, string OrganizationCvr, OrganizationId OrganizationId) : IRequest;
