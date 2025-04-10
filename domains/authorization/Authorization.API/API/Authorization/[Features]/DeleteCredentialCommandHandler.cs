using System;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using MediatR;

namespace API.Authorization._Features_;

public class DeleteCredentialCommandHandler(ICredentialService credentialService, IClientRepository clientRepository)
    : IRequestHandler<DeleteCredentialCommand>
{
    public async Task Handle(DeleteCredentialCommand command, CancellationToken cancellationToken)
    {
        var hasAccess = await clientRepository
            .ExternalClientHasAccessThroughOrganization(command.ClientId, command.OrganizationId);

        if (!hasAccess)
        {
            throw new ForbiddenException();
        }

        await credentialService.DeleteCredential(command.ClientId, command.KeyId, cancellationToken);
    }
}

public record DeleteCredentialCommand(Guid ClientId, Guid KeyId, Guid OrganizationId) : IRequest;
