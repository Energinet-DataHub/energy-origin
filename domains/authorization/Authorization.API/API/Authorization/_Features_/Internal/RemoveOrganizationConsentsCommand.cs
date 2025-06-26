using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class RemoveOrganizationConsentsCommand(Guid organizationId) : IRequest<RemoveOrganizationConsentsCommandResult>
{
    public OrganizationId OrganizationId { get; init; } = OrganizationId.Create(organizationId);
}

public class RemoveOrganizationConsentsCommandResult
{
}

public class RemoveOrganizationConsentsCommandHandler(IOrganizationConsentRepository consentRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveOrganizationConsentsCommand, RemoveOrganizationConsentsCommandResult>
{
    public async Task<RemoveOrganizationConsentsCommandResult> Handle(RemoveOrganizationConsentsCommand request,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var organizationId = request.OrganizationId.Value;
        var organizationConsents = await consentRepository.Query()
            .Where(consent => consent.ConsentGiverOrganizationId == organizationId || consent.ConsentReceiverOrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (organizationConsents.Count > 0)
        {
            consentRepository.RemoveRange(organizationConsents);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return new RemoveOrganizationConsentsCommandResult();
    }
}
