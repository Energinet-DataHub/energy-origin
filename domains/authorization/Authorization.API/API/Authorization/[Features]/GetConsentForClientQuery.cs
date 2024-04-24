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
        if (query.Sub.Equals("529a55d0-68c7-4129-ba3c-e06d4f1038c4"))
            return new (query.Sub,"Granular", "External",new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet");

        return new (default, default, default, default, default);
    }
}

public record GetConsentForClientQuery(string Sub) : IRequest<GetConsentForClientQueryResult>;
public record GetConsentForClientQueryResult(string Sub, string Name, string SubType, IEnumerable<string> OrgIds, string Scope);
