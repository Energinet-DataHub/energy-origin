using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record GetConsentForUserCommand(Guid Sub, string Name, string OrgName, string OrgCvr)
    : IRequest<GetConsentForUserCommandResult>;

public record GetConsentForUserCommandResult(
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
    ITermsRepository termsRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetConsentForUserCommand, GetConsentForUserCommandResult>
{
    public async Task<GetConsentForUserCommandResult> Handle(GetConsentForUserCommand command, CancellationToken cancellationToken)
    {

        const string scope = "dashboard production meters certificates wallet";
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var orgTin = Tin.Create(command.OrgCvr);
            var organization = await organizationRepository.Query()
                .Include(o => o.Affiliations)
                .FirstOrDefaultAsync(o => o.Tin == orgTin, cancellationToken);

            var latestTerms = await termsRepository.Query()
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);


            if (organization == null || latestTerms == null)
            {
                await unitOfWork.RollbackAsync();
                return new GetConsentForUserCommandResult(
                    command.Sub,
                    command.Name,
                    "User",
                    command.OrgName,
                    new List<Guid>(),
                    scope,
                    false
                );
            }

            var termsAccepted = organization.TermsAccepted && organization.TermsVersion == latestTerms.Version;

            if (!termsAccepted)
            {
                await unitOfWork.RollbackAsync();
                return new GetConsentForUserCommandResult(
                    command.Sub,
                    command.Name,
                    "User",
                    command.OrgName,
                    new List<Guid> { organization.Id },
                    scope,
                    false
                );
            }

            var userId = IdpUserId.Create(command.Sub);
            var user = await userRepository.Query()
                .Where(u => u.IdpUserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                user = User.Create(IdpUserId.Create(command.Sub), UserName.Create(command.Name));
                await userRepository.AddAsync(user, cancellationToken);

                if (organization.Affiliations.All(a => a.UserId != user.Id))
                {
                    var affiliation = Affiliation.Create(user, organization);
                    organization.Affiliations.Add(affiliation);
                    organizationRepository.Update(organization);
                }
            }

            await unitOfWork.CommitAsync();

            return new GetConsentForUserCommandResult(
                command.Sub,
                command.Name,
                "User",
                command.OrgName,
                new List<Guid> { organization.Id },
                scope,
                true
            );
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
