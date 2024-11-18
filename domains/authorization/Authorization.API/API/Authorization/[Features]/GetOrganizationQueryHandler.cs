using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrganizationId = API.ValueObjects.OrganizationId;

namespace API.Authorization._Features_;

public class GetOrganizationQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<GetOrganizationQuery, GetOrganizationQueryResult>
{
    public async Task<GetOrganizationQueryResult> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var requestedOrganizationId = request.OrganizationId.Value;
        var org = await organizationRepository.Query().FirstOrDefaultAsync(o => o.Id == requestedOrganizationId, cancellationToken);

        if (org is null)
        {
            throw new EntityNotFoundException(request.OrganizationId.Value.ToString(), nameof(Organization));
        }

        return new GetOrganizationQueryResult(OrganizationId.Create(org.Id), org.Name, org.Tin);
    }
}

public record GetOrganizationQuery(OrganizationId OrganizationId) : IRequest<GetOrganizationQueryResult>;

public record GetOrganizationQueryResult(OrganizationId OrganizationId, OrganizationName OrganizationName, Tin? Tin);