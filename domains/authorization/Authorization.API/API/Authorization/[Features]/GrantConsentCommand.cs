using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GrantConsentCommandHandler(
    IClientRepository clientRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IOrganizationConsentRepository organizationConsentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentCommand>
{
    public async Task Handle(GrantConsentCommand command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(command.UserId);
        var organizationTin = Tin.Create(command.OrganizationCvr);

        var clientOrganizationId = await clientRepository.Query()
            .Where(it => it.IdpClientId == command.IdpClientId)
            .Select(x => x.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        // ?? throw new EntityNotFoundException(command.IdpClientId.Value.ToString(), nameof(Organization));
        if (clientOrganizationId == null)
        {
            throw new Exception("noooo"); // TODO Maybe make this a guard call something something.
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

        var existingConsent = organizationRepository
            .Query()
            .Where(o => o.Id == affiliatedOrganization.Id && o.OrganizationReceivedConsents.Any(c =>
                c.ConsentReceiverOrganization.Clients.Any(x => x.IdpClientId == command.IdpClientId)));

        if (!await existingConsent.AnyAsync(cancellationToken))
        {
            var organizationConsent = OrganizationConsent.Create(affiliatedOrganization.Id, clientOrganizationId.Value, DateTimeOffset.UtcNow);
            await organizationConsentRepository.AddAsync(organizationConsent, cancellationToken);
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentCommand(Guid UserId, string OrganizationCvr, IdpClientId IdpClientId) : IRequest;

public class UserNotAffiliatedWithOrganizationCommandException()
    : ForbiddenException("Not authorized to perform action");
