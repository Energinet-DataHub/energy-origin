using System;
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

public record AcceptTermsCommand(string OrgCvr, Guid UserId, string UserName, string OrgName ) : IRequest<bool>;

public class AcceptTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    ITermsRepository termsRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptTermsCommand, bool>
{
    public async Task<bool> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var orgTin = Tin.Create(request.OrgCvr);
            var organization = await organizationRepository.Query()
                .Include(o => o.Affiliations)
                .FirstOrDefaultAsync(o => o.Tin == orgTin, cancellationToken);

            if (organization == null)
            {
                organization = Organization.Create(orgTin, OrganizationName.Create(request.OrgCvr));
                await organizationRepository.AddAsync(organization, cancellationToken);
            }

            var latestTerms = await termsRepository.Query()
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestTerms == null)
            {
                throw new InvalidOperationException("No terms found in the system");
            }

            if (!organization.TermsAccepted || organization.TermsVersion != latestTerms.Version)
            {
                organization.AcceptTerms(latestTerms);
                organizationRepository.Update(organization);
            }

            var idpUserId = IdpUserId.Create(request.UserId);
            var user = await userRepository.Query()
                .Where(u => u.IdpUserId == idpUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                user = User.Create(idpUserId, UserName.Create(request.UserName));
                await userRepository.AddAsync(user, cancellationToken);

                var affiliation = Affiliation.Create(user, organization);
                organization.Affiliations.Add(affiliation);
                organizationRepository.Update(organization);
            }

            await unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
