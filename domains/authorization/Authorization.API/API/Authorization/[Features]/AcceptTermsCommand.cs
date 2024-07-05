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

public record AcceptTermsCommand(string OrgCvr, string OrgName) : IRequest<bool>;

public class AcceptTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    ITermsRepository termsRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcceptTermsCommand, bool>
{
    public async Task<bool> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var usersOrganizationsCvr = Tin.Create(request.OrgCvr);
            var organization = await organizationRepository.Query()
                .FirstOrDefaultAsync(o => o.Tin == usersOrganizationsCvr, cancellationToken);

            if (organization == null)
            {
                organization = Organization.Create(usersOrganizationsCvr, OrganizationName.Create(request.OrgName));
                await organizationRepository.AddAsync(organization, cancellationToken);
            }

            var latestTerms = await termsRepository.Query()
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestTerms == null)
            {
                return false;
            }

            if (!organization.TermsAccepted || organization.TermsVersion != latestTerms.Version)
            {
                organization.AcceptTerms(latestTerms);
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
