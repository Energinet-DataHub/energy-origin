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

public class DeleteConsentCommandHandler(
    IOrganizationConsentRepository organizationConsentRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteConsentCommand>
{
    public async Task Handle(DeleteConsentCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var idpUserId = IdpUserId.Create(request.IdpUserId);
        var userOrgCvrClaim = Tin.Create(request.OrgCvr);
        var consentId = request.ConsentId;

        var userAffiliation = await userRepository.Query()
            .Where(u => u.IdpUserId == idpUserId)
            .SelectMany(u => u.Affiliations)
            .FirstOrDefaultAsync(a => a.Organization.Tin == userOrgCvrClaim, cancellationToken);

        if (userAffiliation is null)
        {
            throw new ForbiddenException();
        }

        var organizationConsent = await organizationConsentRepository.Query().FirstOrDefaultAsync(x =>
                x.Id == consentId
                 &&
                ((x.ConsentGiverOrganization.Tin == userOrgCvrClaim &&
                x.ConsentGiverOrganization.Affiliations.Any(a => a.User.IdpUserId == idpUserId))
                 ||
                (x.ConsentReceiverOrganization.Tin == userOrgCvrClaim &&
                x.ConsentReceiverOrganization.Affiliations.Any(a => a.User.IdpUserId == idpUserId))),
            cancellationToken);

        if (organizationConsent == null)
        {
            throw new EntityNotFoundException(request.ConsentId, typeof(OrganizationConsent));
        }

        organizationConsentRepository.Remove(organizationConsent);
        await unitOfWork.CommitAsync();
    }
}

public record DeleteConsentCommand(Guid ConsentId, Guid IdpUserId, string OrgCvr) : IRequest;
