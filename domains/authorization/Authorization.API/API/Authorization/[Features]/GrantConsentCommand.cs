using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
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
    IConsentRepository consentRepository,
    IAffiliationRepository affiliationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentCommand>
{
    public async Task Handle(GrantConsentCommand command, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.GetAsync(command.organizationId, cancellationToken);

        var affiliation = await affiliationRepository.Query()
            .Where(a => a.UserId == command.userId && a.OrganizationId == command.organizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (affiliation is null)
        {
            throw new UserNotAffiliatedWithOrganizationCommandException();
        }

        var client = clientRepository.Query()
                         .FirstOrDefault(it => it.IdpClientId == command.idpClientId)
                     ?? throw new EntityNotFoundException(command.idpClientId.Value.ToString(), nameof(Client));

        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);
        await unitOfWork.BeginTransactionAsync();
        await consentRepository.AddAsync(consent, cancellationToken);

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentCommand(Guid userId, Guid organizationId, IdpClientId idpClientId) : IRequest;

public class UserNotAffiliatedWithOrganizationCommandException()
    : ForbiddenException("Not authorized to perform action");
