using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace API.Authorization._Features_;

public class GetConsentForUserQueryHandler : IRequestHandler<GetConsentForUserQuery, GetConsentForUserQueryResult>
{
    public Task<GetConsentForUserQueryResult> Handle(GetConsentForUserQuery query, CancellationToken cancellationToken)
    {
        var result = new GetConsentForUserQueryResult(query.Sub, query.Name, "User", query.OrgName, new[] { Guid.NewGuid() }, "dashboard production meters certificates wallet");
        return Task.FromResult(result);
    }
}

public record GetConsentForUserQuery(Guid Sub, string Name, string OrgName, string OrgCvr) : IRequest<GetConsentForUserQueryResult>;

public record GetConsentForUserQueryResult(Guid Sub, string Name, string SubType, string OrgName, IEnumerable<Guid> OrgIds, string Scope);
