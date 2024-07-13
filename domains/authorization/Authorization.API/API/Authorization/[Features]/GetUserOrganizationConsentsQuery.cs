using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetUserOrganizationConsentsQueryHandler(IConsentRepository consentRepository)
    : IRequestHandler<GetUserOrganizationConsentsQuery, GetUserOrganizationConsentsQueryResult>
{
    public async Task<GetUserOrganizationConsentsQueryResult> Handle(GetUserOrganizationConsentsQuery request, CancellationToken cancellationToken)
    {
        var userIdpUserIdClaim = IdpUserId.Create(Guid.Parse(request.IdpUserId));
        var userOrgCvrClaim = Tin.Create(request.OrgCvr);

        var consents = await consentRepository
            .Query()
            .Where(consent => consent.Organization.Tin == userOrgCvrClaim &&
                              consent.Organization.Affiliations
                                  .Any(o => o.User.IdpUserId == userIdpUserIdClaim))
            .Select(consent => new GetUserOrganizationConsentsQueryResultItem(
                consent.Client.IdpClientId.Value,
                consent.Client.Name.Value,
                UnixTimestamp.Create(consent.ConsentDate).Seconds
            ))
            .ToListAsync(cancellationToken: cancellationToken);

        return new GetUserOrganizationConsentsQueryResult(consents);
    }
}

public record GetUserOrganizationConsentsQuery(string IdpUserId, string OrgCvr) : IRequest<GetUserOrganizationConsentsQueryResult>;

public record GetUserOrganizationConsentsQueryResult(List<GetUserOrganizationConsentsQueryResultItem> Result);

public record GetUserOrganizationConsentsQueryResultItem(Guid IdpClientId, string ClientName, long ConsentDate);
