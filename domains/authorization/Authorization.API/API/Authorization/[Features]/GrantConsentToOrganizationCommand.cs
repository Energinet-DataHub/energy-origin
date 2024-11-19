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
using ForbiddenException = API.Authorization.Exceptions.ForbiddenException;
using OrganizationId = EnergyOrigin.Domain.ValueObjects.OrganizationId;

namespace API.Authorization._Features_;

public class GrantConsentToOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IOrganizationConsentRepository organizationConsentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GrantConsentToOrganizationCommand>
{
    public async Task Handle(GrantConsentToOrganizationCommand command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(command.UserId);
        var organizationTin = Tin.Create(command.OrganizationCvr);
        var consentReceiverOrganizationId = command.OrganizationId.Value;

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
            throw new ForbiddenException();
        }

        if (affiliatedOrganization.Id == command.OrganizationId.Value)
        {
            throw new UnableToGrantConsentToOwnOrganizationException();
        }

        var existingConsent = await organizationConsentRepository
            .Query()
            .Where(oc => oc.ConsentGiverOrganizationId == affiliatedOrganization.Id &&
                         oc.ConsentReceiverOrganizationId == consentReceiverOrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingConsent is null)
        {
            var organizationConsent = OrganizationConsent.Create(affiliatedOrganization.Id, consentReceiverOrganizationId, DateTimeOffset.UtcNow);
            await organizationConsentRepository.AddAsync(organizationConsent, cancellationToken);
        }

        await unitOfWork.CommitAsync();
    }
}

public record GrantConsentToOrganizationCommand(Guid UserId, string OrganizationCvr, OrganizationId OrganizationId) : IRequest;
