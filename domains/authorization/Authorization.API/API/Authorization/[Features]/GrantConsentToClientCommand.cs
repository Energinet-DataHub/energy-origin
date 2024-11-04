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

public class GrantConsentToClientCommandHandler(
    IClientRepository clientRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IOrganizationConsentRepository organizationConsentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentToClientCommand>
{
    public async Task Handle(GrantConsentToClientCommand toClientCommand, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(toClientCommand.UserId);
        var organizationTin = Tin.Create(toClientCommand.OrganizationCvr);

        var clientOrganizationId = await clientRepository.Query()
            .Where(it => it.IdpClientId == toClientCommand.IdpClientId)
            .Select(x => x.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (clientOrganizationId == null)
        {
            throw new EntityNotFoundException(toClientCommand.IdpClientId.Value.ToString(), nameof(Organization));
        }

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
                c.ConsentReceiverOrganization.Clients.Any(x => x.IdpClientId == toClientCommand.IdpClientId))).AnyAsync(cancellationToken);

        if (!existingConsent)
        {
            var organizationConsent = OrganizationConsent.Create(affiliatedOrganization.Id, clientOrganizationId.Value, DateTimeOffset.UtcNow);
            await organizationConsentRepository.AddAsync(organizationConsent, cancellationToken);
        }
        else
        {
            throw new EntityAlreadyExistsException(nameof(OrganizationConsent));
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentToClientCommand(Guid UserId, string OrganizationCvr, IdpClientId IdpClientId) : IRequest;

public class UserNotAffiliatedWithOrganizationCommandException() : ForbiddenException("Not authorized to perform action");
