using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using API.Data;
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
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var organization = await organizationRepository.Query()
                .Include(o => o.Affiliations)
                .FirstOrDefaultAsync(o => o.Tin.Value == command.OrgCvr, cancellationToken);

            var latestTerms = await termsRepository.Query().LastOrDefaultAsync(cancellationToken);
            var termsAccepted = false;

            if (organization == null)
            {
                await unitOfWork.RollbackAsync();
                return new GetConsentForUserCommandResult(
                    command.Sub,
                    command.Name,
                    "User",
                    command.OrgName,
                    new List<Guid>(),
                    "dashboard production meters certificates wallet",
                    termsAccepted
                );
            }

            termsAccepted = organization.TermsAccepted && organization.TermsVersion == latestTerms?.Version;

            if (!termsAccepted)
            {
                await unitOfWork.RollbackAsync();
                return new GetConsentForUserCommandResult(
                    command.Sub,
                    command.Name,
                    "User",
                    command.OrgName,
                    organization.Affiliations.Select(a => a.UserId),
                    "dashboard production meters certificates wallet",
                    termsAccepted
                );
            }

            var user = await userRepository.Query()
                .FirstOrDefaultAsync(u => u.IdpUserId.Value == command.Sub, cancellationToken);

            if (user == null)
            {
                user = User.Create(IdpUserId.Create(command.Sub), UserName.Create(command.Name));
                await userRepository.AddAsync(user, cancellationToken);

                var affiliation = Affiliation.Create(user, organization);
                organization.Affiliations.Add(affiliation);
                organizationRepository.Update(organization);

                await unitOfWork.CommitAsync();
            }
            else
            {
                await unitOfWork.RollbackAsync(); // No changes needed
            }

            return new GetConsentForUserCommandResult(
                command.Sub,
                command.Name,
                "User",
                command.OrgName,
                organization.Affiliations.Select(a => a.UserId),
                "dashboard production meters certificates wallet",
                termsAccepted
            );
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
