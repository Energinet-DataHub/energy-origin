using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
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
    public async Task Handle(GrantConsentToClientCommand command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(command.IdpUserId);
        var organizationTin = Tin.Create(command.OrganizationCvr);

        var clientOrganizationId = await clientRepository.Query()
            .Where(it => it.IdpClientId == command.IdpClientId)
            .Select(x => x.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (clientOrganizationId == null)
        {
            throw new EntityNotFoundException(command.IdpClientId.Value.ToString(), nameof(Client));
        }

        var affiliatedOrganization = await userRepository.Query()
            .Where(u => u.IdpUserId == idpUserId)
            .SelectMany(u => u.Affiliations)
            .Where(a => a.Organization.Tin == organizationTin)
            .Select(a => a.Organization)
            .FirstOrDefaultAsync(cancellationToken);

        if (affiliatedOrganization is null)
        {
            throw new ForbiddenException();
        }

        if (clientOrganizationId == affiliatedOrganization.Id)
        {
            throw new UnableToGrantConsentToOwnOrganizationException();
        }

        var existingConsent = organizationRepository
            .Query()
            .Where(o => o.Id == affiliatedOrganization.Id && o.OrganizationGivenConsents.Any(c =>
                c.ConsentReceiverOrganization.Clients.Any(x => x.IdpClientId == command.IdpClientId)));

        if (!await existingConsent.AnyAsync(cancellationToken))
        {
            var organizationConsent = OrganizationConsent.Create(affiliatedOrganization.Id, clientOrganizationId.Value, DateTimeOffset.UtcNow);
            await organizationConsentRepository.AddAsync(organizationConsent, cancellationToken);
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentToClientCommand(Guid IdpUserId, string OrganizationCvr, IdpClientId IdpClientId) : IRequest;
