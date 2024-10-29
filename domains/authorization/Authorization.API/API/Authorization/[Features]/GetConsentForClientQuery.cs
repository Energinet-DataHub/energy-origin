using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetConsentForClientQueryHandler : IRequestHandler<GetConsentForClientQuery, GetConsentForClientQueryResult>
{
    private readonly IClientRepository _clientRepository;
    private const string Scope = "dashboard production meters certificates wallet";

    public GetConsentForClientQueryHandler(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query,
        CancellationToken cancellationToken)
    {
        var requestedClientId = new IdpClientId(query.IdpClientId);

        var client = await _clientRepository.Query()
            .Where(client => client.IdpClientId == requestedClientId)
            .Select(client =>
                new GetConsentForClientQueryResult(
                    query.IdpClientId,
                    client.ClientType.ToString(),
                    client.Name.Value,
                client.Organization!.OrganizationReceivedConsents.Select(x => x.ConsentGiverOrganizationId),
                 Guid.Empty,
                    Scope)
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new EntityNotFoundException(query.IdpClientId, typeof(Client));
        }

        return client;
    }
}

public record GetConsentForClientQuery(Guid IdpClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(
    Guid Sub,
    string SubType,
    string OrgName,
    IEnumerable<Guid> OrgIds,
    Guid OrgId,
    string Scope);
