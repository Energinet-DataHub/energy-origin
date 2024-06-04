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

    public GetConsentForClientQueryHandler(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query,
        CancellationToken cancellationToken)
    {
        var requestedClientId = new IdpClientId(query.ClientId);

        var client = await _clientRepository.Query()
            .Where(x => x.IdpClientId == requestedClientId)
            .Select(x => new GetConsentForClientQueryResult(query.ClientId, x.ClientType.ToString(), "someOrgName",
                new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
                "dashboard production meters certificates wallet"))
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new EntityNotFoundException(query.ClientId, typeof(Client));
        }

        return client;
    }
}

public record GetConsentForClientQuery(Guid ClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(
    Guid Sub,
    string SubType,
    string OrgName,
    IEnumerable<Guid> OrgIds,
    string Scope);
