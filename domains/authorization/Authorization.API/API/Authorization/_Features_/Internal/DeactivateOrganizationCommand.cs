using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Authorization._Features_.Internal;

public record DeactivateOrganizationCommand(Guid OrganizationId) : IRequest<DeactivateOrganizationCommandResult>;

public class DeactivateOrganizationCommandResult { }

public class DeactivateOrganizationCommandHandler(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateOrganizationCommandHandler> logger
) : IRequestHandler<DeactivateOrganizationCommand, DeactivateOrganizationCommandResult>
{
    public async Task<DeactivateOrganizationCommandResult> Handle(
        DeactivateOrganizationCommand request,
        CancellationToken cancellationToken
    )
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization is not null)
        {
            organization.Deactivate();
        }
        else
        {
            logger.LogWarning("This should never happen - You are trying to deactivate a non-existent organization");
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return new DeactivateOrganizationCommandResult();
    }
}
