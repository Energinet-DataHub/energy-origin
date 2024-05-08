using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
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

    public async Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query, CancellationToken cancellationToken)
    {
        var result = await _clientRepository.Query()
            .Where(x => x.IdpClientId.Value == query.ClientId)
            .Select(x => new GetConsentForClientQueryResult(query.ClientId, x.Role.ToString(), "someOrgName", new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet"))
            .FirstAsync(cancellationToken);

        if (result.OrgName == "Energinet")
        {
            throw new Exception("Nooooooh!");
        }

        return result;
    }
}

public record GetConsentForClientQuery(Guid ClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(Guid Sub, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
