using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class DeleteConsentCommandHandler(
    IConsentRepository consentRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteConsentCommand, bool>
{
    public async Task<bool> Handle(DeleteConsentCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var consent = await consentRepository.Query()
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId && c.OrganizationId == request.OrganizationId, cancellationToken);

        if (consent == null)
        {
            return false;
        }

        var idpUserId = IdpUserId.Create(Guid.Parse(request.UserId));

        var userAffiliation = await userRepository.Query()
            .Where(u => u.IdpUserId == idpUserId)
            .SelectMany(u => u.Affiliations)
            .FirstOrDefaultAsync(a => a.OrganizationId == request.OrganizationId, cancellationToken);

        if (userAffiliation == null || consent.Organization.Tin.Value != request.OrgCvr)
        {
            throw new UserNotAffiliatedWithOrganizationCommandException();
        }

        consentRepository.Remove(consent);
        await unitOfWork.CommitAsync();

        return true;
    }
}

public record DeleteConsentCommand(Guid ClientId, Guid OrganizationId, string UserId, string OrgCvr) : IRequest<bool>;
