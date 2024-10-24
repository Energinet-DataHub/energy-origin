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

public class GetUserOrganizationConsentsReceivedQueryHandler(IOrganizationConsentRepository organizationConsentRepository)
    : IRequestHandler<GetUserOrganizationConsentsReceivedQuery, GetUserOrganizationConsentsReceivedQueryResult>
{
    public async Task<GetUserOrganizationConsentsReceivedQueryResult> Handle(GetUserOrganizationConsentsReceivedQuery request, CancellationToken cancellationToken)
    {
        var userIdpUserIdClaim = IdpUserId.Create(Guid.Parse(request.IdpUserId));
        var userOrgCvrClaim = Tin.Create(request.OrgCvr);

        var consents = await organizationConsentRepository
            .Query()
            .Where(consent => consent.ConsentReceiverOrganization.Tin == userOrgCvrClaim &&
                              consent.ConsentReceiverOrganization.Affiliations
                                  .Any(o => o.User.IdpUserId == userIdpUserIdClaim))
            .Select(consent =>
                new GetUserOrganizationConsentsReceivedQueryResultItem(
                    consent.Id,
                    consent.ConsentGiverOrganizationId,
                    consent.ConsentGiverOrganization.Tin!.Value,
                    consent.ConsentGiverOrganization.Name.Value,
                    UnixTimestamp.Create(consent.ConsentDate).Seconds)
            )
            .ToListAsync(cancellationToken: cancellationToken);

        return new GetUserOrganizationConsentsReceivedQueryResult(consents);
    }
}

public record GetUserOrganizationConsentsReceivedQuery(string IdpUserId, string OrgCvr) : IRequest<GetUserOrganizationConsentsReceivedQueryResult>;

public record GetUserOrganizationConsentsReceivedQueryResult(List<GetUserOrganizationConsentsReceivedQueryResultItem> Result);

public record GetUserOrganizationConsentsReceivedQueryResultItem(Guid ConsentId, Guid OrganizationId, string? OrganizationTin, string OrganizationName, long ConsentDate);
