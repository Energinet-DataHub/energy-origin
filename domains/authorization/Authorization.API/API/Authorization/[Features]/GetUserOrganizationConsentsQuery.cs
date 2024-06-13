using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using API.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GetUserOrganizationConsentsQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserOrganizationConsentsQuery, GetUserOrganizationConsentsQueryResult>
{
    public async Task<GetUserOrganizationConsentsQueryResult> Handle(GetUserOrganizationConsentsQuery request, CancellationToken cancellationToken)
    {
        var idpUserId = IdpUserId.Create(Guid.Parse(request.IdpUserId));

        var user = await userRepository.Query()
            .Include(u => u.Affiliations)
            .ThenInclude(a => a.Organization)
            .ThenInclude(o => o.Consents)
            .FirstOrDefaultAsync(u => u.IdpUserId == idpUserId, cancellationToken);

        if (user == null)
            throw new Exception("User not found");

        var organization = user.Affiliations.Single().Organization;

        var consents = organization.Consents.Select(c =>
                new ConsentDto(c.Client.IdpClientId, organization.Name, c.Client.RedirectUrl))
            .ToList();

        return new GetUserOrganizationConsentsQueryResult(consents);
    }
}

public record GetUserOrganizationConsentsQuery(string IdpUserId) : IRequest<GetUserOrganizationConsentsQueryResult>;

public record GetUserOrganizationConsentsQueryResult(List<ConsentDto> Result);
