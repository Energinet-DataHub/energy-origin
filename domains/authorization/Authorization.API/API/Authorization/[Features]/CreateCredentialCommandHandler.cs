using System;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using MediatR;

namespace API.Authorization._Features_;

public class CreateCredentialCommandHandler(ICredentialService credentialService, IClientRepository clientRepository)
    : IRequestHandler<CreateCredentialCommand, CreateCredentialCommandResult>
{
    public async Task<CreateCredentialCommandResult> Handle(CreateCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var hasAccess = await clientRepository
            .ExternalClientHasAccessThroughOrganization(command.ClientId, command.OrganizationId);

        if (!hasAccess)
        {
            throw new ForbiddenException();
        }

        var credential = await credentialService.CreateCredential(command.ClientId, cancellationToken);
        return new CreateCredentialCommandResult(credential.Hint, credential.KeyId, credential.StartDateTime,
            credential.EndDateTime, credential.Secret);
    }
}

public record CreateCredentialCommand(Guid ClientId, Guid OrganizationId) : IRequest<CreateCredentialCommandResult>;

public record CreateCredentialCommandResult(
    string? Hint,
    Guid KeyId,
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime,
    string? Secret);
