using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record OrganizationStateQuery(string Tin) : IRequest<bool>;

public class OrganizationStateQueryHandler(
    IOrganizationRepository organizationRepository,
    ITermsRepository termsRepository)
    : IRequestHandler<OrganizationStateQuery, bool>
{
    public async Task<bool> Handle(OrganizationStateQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin.Value == request.Tin, cancellationToken);

        if (organization == null)
            return false;

        var latestTerms = await termsRepository.Query()
            .LastOrDefaultAsync(cancellationToken);

        return organization.TermsAccepted && organization.TermsVersion == latestTerms?.Version;
    }
}
