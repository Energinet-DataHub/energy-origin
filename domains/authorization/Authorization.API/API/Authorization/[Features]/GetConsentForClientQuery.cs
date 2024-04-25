using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Controllers;
using MediatR;

namespace API.Authorization._Features_;

public class GetConsentForClientQueryHandler : IRequestHandler<GetConsentForClientQuery, GetConsentForClientQueryResult>
{
    public async Task<GetConsentForClientQueryResult> Handle(GetConsentForClientQuery query, CancellationToken cancellationToken)
    {
        if (query.ClientId.Equals("529a55d0-68c7-4129-ba3c-e06d4f1038c4"))
            return new(query.ClientId, "Granular System", "External", "Granular", new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet");

        return new(default, default, default, default, default, default);
    }
}

public record GetConsentForClientQuery(string ClientId) : IRequest<GetConsentForClientQueryResult>;

public record GetConsentForClientQueryResult(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
