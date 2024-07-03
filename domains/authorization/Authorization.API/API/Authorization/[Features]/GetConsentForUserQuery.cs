using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using API.Repository;
using API.Models;
using API.ValueObjects;

namespace API.Authorization._Features_;

public record GetConsentForUserQuery(Guid Sub, string Name, string OrgName, string OrgCvr)
    : IRequest<GetConsentForUserQueryResult>;

public record GetConsentForUserQueryResult(
    Guid Sub,
    string Name,
    string SubType,
    string OrgName,
    IEnumerable<Guid> OrgIds,
    string Scope,
    bool TermsAccepted);

public class GetConsentForUserQueryHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    ITermsRepository termsRepository)
    : IRequestHandler<GetConsentForUserQuery, GetConsentForUserQueryResult>
{
    public async Task<GetConsentForUserQueryResult> Handle(GetConsentForUserQuery query, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.Query()
            .Include(o => o.Affiliations)
            .FirstOrDefaultAsync(o => o.Tin.Value == query.OrgCvr, cancellationToken);

        var latestTerms = await termsRepository.Query().LastOrDefaultAsync(cancellationToken);
        var termsAccepted = false;

        if (organization == null)
            return new GetConsentForUserQueryResult(
                query.Sub,
                query.Name,
                "User",
                query.OrgName,
                organization?.Affiliations.Select(a => a.UserId) ?? new List<Guid>(),
                "dashboard production meters certificates wallet",
                termsAccepted
            );
        termsAccepted = organization.TermsAccepted && organization.TermsVersion == latestTerms?.Version;

        if (!termsAccepted)
            return new GetConsentForUserQueryResult(
                query.Sub,
                query.Name,
                "User",
                query.OrgName,
                organization?.Affiliations.Select(a => a.UserId) ?? new List<Guid>(),
                "dashboard production meters certificates wallet",
                termsAccepted
            );
        var user = await userRepository.Query()
            .FirstOrDefaultAsync(u => u.IdpUserId.Value == query.Sub, cancellationToken);

        if (user != null)
            return new GetConsentForUserQueryResult(
                query.Sub,
                query.Name,
                "User",
                query.OrgName,
                organization?.Affiliations.Select(a => a.UserId) ?? new List<Guid>(),
                "dashboard production meters certificates wallet",
                termsAccepted
            );
        user = User.Create(IdpUserId.Create(query.Sub), UserName.Create(query.Name));
        await userRepository.AddAsync(user, cancellationToken);

        var affiliation = Affiliation.Create(user, organization);
        organization.Affiliations.Add(affiliation);
        organizationRepository.Update(organization);

        return new GetConsentForUserQueryResult(
            query.Sub,
            query.Name,
            "User",
            query.OrgName,
            organization?.Affiliations.Select(a => a.UserId) ?? new List<Guid>(),
            "dashboard production meters certificates wallet",
            termsAccepted
        );
    }
}
