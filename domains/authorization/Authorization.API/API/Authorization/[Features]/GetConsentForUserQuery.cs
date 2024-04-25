using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace API.Authorization._Features_;

public class GetConsentForUserQueryHandler : IRequestHandler<GetConsentForUserQuery, GetConsentForUserQueryResult>
{
    public async Task<GetConsentForUserQueryResult> Handle(GetConsentForUserQuery query, CancellationToken cancellationToken)
    {
        return new(query.Sub, query.Name, "User", query.OrgName, new[] { Guid.NewGuid().ToString() }, "dashboard production meters certificates wallet");
    }
}

public record GetConsentForUserQuery(string Sub, string Name, string OrgName, string OrgCvr) : IRequest<GetConsentForUserQueryResult>;
public record GetConsentForUserQueryResult(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);


