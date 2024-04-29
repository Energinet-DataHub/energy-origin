using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Controllers;
using MediatR;

namespace API.Authorization._Features_;

public class GetConsentForClientQueryHandler : IRequestHandler<GetConsentForClientQuery, GetConsentForClientQueryResult>
{
    public Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query, CancellationToken cancellationToken)
    {
        GetConsentForClientQueryResult result;
        if (query.ClientId.Equals("529a55d0-68c7-4129-ba3c-e06d4f1038c4"))
        {
            result = new GetConsentForClientQueryResult(query.ClientId, "Granular System", "External", "Granular", new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet");
        }
        else
        {
            result = new GetConsentForClientQueryResult(default!, default!, default!, default!, default!, default!);
        }

        return Task.FromResult(result);
    }
}

public record GetConsentForClientQuery(string ClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
