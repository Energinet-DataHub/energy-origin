using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record OrganizationStateQuery(string Tin) : IRequest<bool>;

public class OrganizationStateQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<OrganizationStateQuery, bool>
{
    public async Task<bool> Handle(OrganizationStateQuery request, CancellationToken cancellationToken)
    {
        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin.Value == request.Tin, cancellationToken);

        return organization?.TermsAccepted ?? false;
    }
}
