using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Metrics;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetConsentForClientQueryHandler : IRequestHandler<GetConsentForClientQuery, GetConsentForClientQueryResult>
{
    private readonly IClientRepository _clientRepository;
    private readonly IAuthorizationMetrics _metrics;

    private const string Scope = "dashboard production meters certificates wallet";

    public GetConsentForClientQueryHandler(IClientRepository clientRepository, IAuthorizationMetrics metrics)
    {
        _clientRepository = clientRepository;
        _metrics = metrics;
    }

    public async Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query,
        CancellationToken cancellationToken)
    {
        // TODO: CABOL - We need to handle internal and external clients. Only external clients can be trial or non trial. It does not make sense for internal clients
        // TODO: CABOL - Change database layout or do something different?

        var requestedClientId = new IdpClientId(query.IdpClientId);

        var client = await _clientRepository.Query()
            .Where(client => client.IdpClientId == requestedClientId)
            .Select(client =>
                new GetConsentForClientQueryResult(
                    query.IdpClientId,
                    client.ClientType.ToString(),
                    client.Name.Value,
                    client.Organization!.OrganizationReceivedConsents
                        .Where(x => client.IsTrial ? x.ConsentGiverOrganization.Status == OrganizationStatus.Trial : x.ConsentGiverOrganization.Status == OrganizationStatus.Normal)
                        .Select(x => x.ConsentGiverOrganizationId),
                    client.Organization.Id,
                    Scope)
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new EntityNotFoundException(query.IdpClientId, typeof(Client));
        }

        _metrics.AddUniqueClientOrganizationLogin(client.OrgId.ToString());
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
