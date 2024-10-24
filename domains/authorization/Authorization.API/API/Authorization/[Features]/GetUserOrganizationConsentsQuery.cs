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

public class GetUserOrganizationConsentsQueryHandler(IOrganizationConsentRepository organizationConsentRepository)
    : IRequestHandler<GetUserOrganizationConsentsQuery, GetUserOrganizationConsentsQueryResult>
{
    public async Task<GetUserOrganizationConsentsQueryResult> Handle(GetUserOrganizationConsentsQuery request, CancellationToken cancellationToken)
    {
        var userIdpUserIdClaim = IdpUserId.Create(Guid.Parse(request.IdpUserId));
        var userOrgCvrClaim = Tin.Create(request.OrgCvr);

        var consents = await organizationConsentRepository
            .Query()
            .Where(consent => (consent.ConsentGiverOrganization.Tin == userOrgCvrClaim &&
                              consent.ConsentGiverOrganization.Affiliations
                                  .Any(o => o.User.IdpUserId == userIdpUserIdClaim))
                                ||
                                (consent.ConsentReceiverOrganization.Tin == userOrgCvrClaim &&
                                 consent.ConsentReceiverOrganization.Affiliations
                                     .Any(o => o.User.IdpUserId == userIdpUserIdClaim))
            )
            .Select(consent =>
                new GetUserOrganizationConsentsQueryResultItem(
                    consent.Id,
                    consent.ConsentGiverOrganizationId,
                    consent.ConsentGiverOrganization.Tin!.Value,
                    consent.ConsentGiverOrganization.Name.Value,
                    consent.ConsentReceiverOrganizationId,
                    consent.ConsentReceiverOrganization.Tin!.Value,
                    consent.ConsentReceiverOrganization.Name.Value,
                    UnixTimestamp.Create(consent.ConsentDate).Seconds)
            )
            .ToListAsync(cancellationToken: cancellationToken);

        return new GetUserOrganizationConsentsQueryResult(consents);
    }
}

public record GetUserOrganizationConsentsQuery(string IdpUserId, string OrgCvr) : IRequest<GetUserOrganizationConsentsQueryResult>;

public record GetUserOrganizationConsentsQueryResult(List<GetUserOrganizationConsentsQueryResultItem> Result);

public record GetUserOrganizationConsentsQueryResultItem(Guid ConsentId, Guid GiverOrganizationId, string GiverOrganizationTin, string GiverOrganizationName, Guid ReceiverOrganizationId, string ReceiverOrganizationTin, string ReceiverOrganizationName, long ConsentDate);
