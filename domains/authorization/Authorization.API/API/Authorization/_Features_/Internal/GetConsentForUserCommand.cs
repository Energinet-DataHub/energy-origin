using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Metrics;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record GetConsentForUserCommand(Guid Sub, string Name, string OrgName, string OrgCvr)
    : IRequest<GetConsentForUserCommandResult>;

public record GetConsentForUserCommandResult(
    Guid Sub,
    string Name,
    string SubType,
    string OrgName,
    Guid OrgId,
    IEnumerable<Guid> OrgIds,
    string Scope,
    bool TermsAccepted);

public class GetConsentForUserQueryHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    ITermsRepository termsRepository,
    IUnitOfWork unitOfWork,
    IAuthorizationMetrics metrics)

    : IRequestHandler<GetConsentForUserCommand, GetConsentForUserCommandResult>
{
    public async Task<GetConsentForUserCommandResult> Handle(GetConsentForUserCommand command, CancellationToken cancellationToken)
    {
        const string scope = "dashboard production meters certificates wallet";
        const string subType = "User";
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var orgTin = Tin.Create(command.OrgCvr);
        var organization = await organizationRepository.Query()
            .Include(o => o.Affiliations)
            .FirstOrDefaultAsync(o => o.Tin == orgTin, cancellationToken);

        if (organization is null)
        {
            return new GetConsentForUserCommandResult(
                command.Sub,
                command.Name,
                subType,
                command.OrgName,
                Guid.Empty,
                new List<Guid>(),
                scope,
                false);
        }

        var termsType = organization.Status == OrganizationStatus.Trial
            ? TermsType.Trial
            : TermsType.Normal;

        var terms = await termsRepository.Query()
            .Where(t => t.Type == termsType)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (terms is null)
        {
            return new GetConsentForUserCommandResult(
                command.Sub,
                command.Name,
                subType,
                command.OrgName,
                organization.Id,
                new List<Guid>(),
                scope,
                false);
        }

        var latestTermsAccepted =
            organization.TermsAccepted &&
            organization.TermsVersion == terms.Version;

        if (!latestTermsAccepted)
        {
            return new GetConsentForUserCommandResult(
                command.Sub,
                command.Name,
                subType,
                command.OrgName,
                organization.Id,
                new List<Guid>(),
                scope,
                false);
        }

        var userId = IdpUserId.Create(command.Sub);
        var user = await userRepository.Query()
            .Where(u => u.IdpUserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            user = User.Create(IdpUserId.Create(command.Sub), UserName.Create(command.Name));
            await userRepository.AddAsync(user, cancellationToken);
            _ = Affiliation.Create(user, organization);
        }
        else if (organization.Affiliations.All(a => a.UserId != user.Id))
        {
            _ = Affiliation.Create(user, organization);
            organizationRepository.Update(organization);
            userRepository.Update(user);
        }

        var consentReceivedOrganizationIds = await organizationRepository.Query()
            .Where(org => org.Id == organization.Id)
            .SelectMany(org => org.OrganizationReceivedConsents)
            .Select(consent => consent.ConsentGiverOrganizationId)
            .ToListAsync(cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        metrics.AddUniqueUserLogin(organization.Id.ToString(), user.IdpUserId);

        return new GetConsentForUserCommandResult(
            command.Sub,
            command.Name,
            subType,
            command.OrgName,
            organization.Id,
            consentReceivedOrganizationIds,
            scope,
            true
        );
    }
}
