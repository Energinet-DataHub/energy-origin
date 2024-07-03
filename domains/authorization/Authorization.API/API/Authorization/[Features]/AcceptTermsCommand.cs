using System;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record AcceptTermsCommand(string OrgCvr, Guid UserId, string UserName) : IRequest<bool>;

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
            var organization = await organizationRepository.Query()
                .FirstOrDefaultAsync(o => o.Tin.Value == request.OrgCvr, cancellationToken);

            if (organization == null)
            {
                organization = Organization.Create(new Tin(request.OrgCvr), new OrganizationName(request.OrgCvr));
                await organizationRepository.AddAsync(organization, cancellationToken);
            }

            var latestTerms = await termsRepository.Query().LastOrDefaultAsync(cancellationToken);

            if (latestTerms == null)
            {
                throw new InvalidOperationException("No terms found in the system");
            }

            if (!organization.TermsAccepted || organization.TermsVersion != latestTerms.Version)
            {
                organization.AcceptTerms(latestTerms);
                organizationRepository.Update(organization);
            }

            var user = await userRepository.Query()
                .FirstOrDefaultAsync(u => u.IdpUserId.Value == request.UserId, cancellationToken);

            if (user == null)
            {
                user = User.Create(IdpUserId.Create(request.UserId), UserName.Create(request.UserName));
                await userRepository.AddAsync(user, cancellationToken);
            }

            var affiliation = Affiliation.Create(user, organization);
            organization.Affiliations.Add(affiliation);
            organizationRepository.Update(organization);

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
