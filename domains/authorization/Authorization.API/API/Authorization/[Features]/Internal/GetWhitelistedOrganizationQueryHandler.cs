using System.Threading;
using System.Threading.Tasks;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetWhitelistedOrganizationQueryHandler(IWhitelistedRepository whitelistedRepository)
    : IRequestHandler<GetWhitelistedOrganizationQuery, bool>
{
    public Task<bool> Handle(GetWhitelistedOrganizationQuery request, CancellationToken cancellationToken)
    {
        return whitelistedRepository.Query().AnyAsync(w => w.Tin == Tin.Create(request.Tin), cancellationToken);
    }
}

public record GetWhitelistedOrganizationQuery(string Tin) : IRequest<bool>;
