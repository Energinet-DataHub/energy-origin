using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist;
using EnergyOrigin.IntegrationEvents.Events.OrganizationRemovedFromWhitelist.V1;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class RemoveFromWhitelistCommand(string tin) : IRequest<RemoveFromWhitelistCommandResult>
{
    public Tin Tin { get; init; } = Tin.Create(tin);
}

public class RemoveFromWhitelistCommandResult
{
}

public class RemoveOrganizationFromWhitelistCommandHandler(
    IWhitelistedRepository whitelistRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveFromWhitelistCommand, RemoveFromWhitelistCommandResult>
{
    public async Task<RemoveFromWhitelistCommandResult> Handle(RemoveFromWhitelistCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var existingWhitelistEntry = await whitelistRepository.Query().FirstOrDefaultAsync(wl => wl.Tin == request.Tin, cancellationToken);

        if (existingWhitelistEntry is null)
        {
            return new RemoveFromWhitelistCommandResult();
        }

        whitelistRepository.Remove(existingWhitelistEntry);

        var removedEvent = OrganizationRemovedFromWhitelist.Create(existingWhitelistEntry.Id, existingWhitelistEntry.Tin.Value);
        await publishEndpoint.Publish(removedEvent, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return new RemoveFromWhitelistCommandResult();
    }
}
