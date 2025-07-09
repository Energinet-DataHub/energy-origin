using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Repository;
using API.Services;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Authorization._Features_.Internal;

public class RemoveOrganizationClientsCommand(Guid organizationId) : IRequest<RemoveOrganizationClientsCommandResult>
{
    public OrganizationId OrganizationId { get; init; } = OrganizationId.Create(organizationId);
}

public class RemoveOrganizationClientsCommandResult
{
}

public class RemoveOrganizationClientsCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork, IGraphServiceClientWrapper graphServiceClientWrapper, ILogger<RemoveOrganizationClientsCommandHandler> logger)
    : IRequestHandler<RemoveOrganizationClientsCommand, RemoveOrganizationClientsCommandResult>
{
    public async Task<RemoveOrganizationClientsCommandResult> Handle(RemoveOrganizationClientsCommand request,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var organizationId = request.OrganizationId.Value;
        var organizationClients = await clientRepository.Query()
            .Where(client => client.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (organizationClients.Count > 0)
        {
            foreach (var client in organizationClients)
            {
                try
                {
                    await graphServiceClientWrapper.DeleteApplication(client.IdpClientId.Value.ToString(), cancellationToken);
                    logger.LogInformation("Deleted Azure B2C app registration for client {ClientId}", client.IdpClientId.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete Azure B2C app registration for client {ClientId}", client.IdpClientId.Value);
                }
            }

            clientRepository.RemoveRange(organizationClients);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return new RemoveOrganizationClientsCommandResult();
    }
}
