using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace API.Authorization._Features_;

public class GetConsentForUserQueryHandler : IRequestHandler<GetConsentForUserQuery, GetConsentForUserQueryResult>
{
    public async Task<GetConsentForUserQueryResult> Handle(GetConsentForUserQuery query, CancellationToken cancellationToken)
    {
        if (query.Sub.Equals("529a55d0-68c7-4129-ba3c-e06d4f1038c4"))
            return new (query.Sub,query.Name, "User", query.OrgName ,new[] { query.OrgId }, "dashboard production meters certificates wallet");

        return new GetConsentForUserQueryResult(default, default, default, default, default, default);
    }
}

public record GetConsentForUserQuery(string Sub, string Name, string OrgName, string OrgId) : IRequest<GetConsentForUserQueryResult>;
public record GetConsentForUserQueryResult(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);


