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

public class GrantConsentCommandHandler(
    IClientRepository clientRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentCommand>
{
    public async Task Handle(GrantConsentCommand command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(command.userId);

        var client = clientRepository.Query()
                         .FirstOrDefault(it => it.IdpClientId == command.idpClientId)
                     ?? throw new EntityNotFoundException(command.idpClientId.Value.ToString(), nameof(Client));

        var affiliatedOrganization = await userRepository.Query()
            .Where(u => u.IdpUserId == idpUserId)
            .SelectMany(u => u.Affiliations)
            .Where(a => a.Organization.Id == command.organizationId)
            .Select(a => a.Organization)
            .FirstOrDefaultAsync(cancellationToken);

        if (affiliatedOrganization is null)
        {
            throw new UserNotAffiliatedWithOrganizationCommandException();
        }

        var existingConsent = organizationRepository.Query().Where(o => o == affiliatedOrganization && o.Consents.Any(c => c.Client == client))
            .SelectMany(o => o.Consents)
            .FirstOrDefault();

        if (existingConsent is null)
        {
            _ = Consent.Create(affiliatedOrganization, client, DateTimeOffset.UtcNow);
            clientRepository.Update(client);
            organizationRepository.Update(affiliatedOrganization);
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentCommand(Guid userId, Guid organizationId, IdpClientId idpClientId) : IRequest;

public class UserNotAffiliatedWithOrganizationCommandException()
    : ForbiddenException("Not authorized to perform action");
