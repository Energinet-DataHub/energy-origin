using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Controllers;
using API.Data;
using API.Models;
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
            .Where(x => x.IdpClientId.Value == new Guid(query.ClientId))
            .Select(x => new GetConsentForClientQueryResult(query.ClientId, x.Name.Value, x.Role.ToString(), x.Name.Value, new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet"))
            .FirstAsync(cancellationToken);

        if (result.OrgName == "Energinet")
        {
            throw new Exception("Nooooooh!");
        }

        return result;
    }
}

public record GetConsentForClientQuery(string ClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
