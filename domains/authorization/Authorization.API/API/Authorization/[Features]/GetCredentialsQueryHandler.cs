using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.Services;
using EnergyOrigin.Setup.Exceptions;
using MediatR;

namespace API.Authorization._Features_;

public class GetCredentialsQueryHandler(ICredentialService credentialService, IClientRepository clientRepository)
    : IRequestHandler<GetCredentialsQuery, IEnumerable<GetCredentialsQueryResult>>
{
    public async Task<IEnumerable<GetCredentialsQueryResult>> Handle(GetCredentialsQuery request,
        CancellationToken cancellationToken)
    {
        var hasAccess = await clientRepository
            .ExternalClientHasAccessThroughOrganization(request.ClientId, request.OrganizationId);

        if (!hasAccess)
        {
            throw new ForbiddenException();
        }

        var credentials = await credentialService.GetCredentials(request.ClientId, cancellationToken);

        return credentials.Select(credential => new GetCredentialsQueryResult(credential.Hint, credential.KeyId,
            credential.StartDateTime, credential.EndDateTime)).ToList();
    }
}

public record GetCredentialsQuery(Guid ClientId, Guid OrganizationId) : IRequest<IEnumerable<GetCredentialsQueryResult>>;

public record GetCredentialsQueryResult(
    string? Hint,
    Guid KeyId,
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime);
