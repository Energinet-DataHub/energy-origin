using System;
using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class IsWhitelistedOrganizationQueryHandler(IWhitelistedRepository whitelistedRepository)
    : IRequestHandler<IsWhitelistedOrganizationQuery, bool>
{
    public Task<bool> Handle(IsWhitelistedOrganizationQuery request, CancellationToken cancellationToken)
    {
        return whitelistedRepository.Query().AnyAsync(w => w.Tin.Value == request.Tin, cancellationToken);
    }
}

public record IsWhitelistedOrganizationQuery(string Tin) : IRequest<bool>;
