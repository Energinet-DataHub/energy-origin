using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetOrganizationStatusQueryHandler(
    IOrganizationRepository organizationRepository,
    IWhitelistedRepository whitelistedRepository)
    : IRequestHandler<GetOrganizationStatusQuery, bool>
{
    public async Task<bool> Handle(GetOrganizationStatusQuery request, CancellationToken cancellationToken)
    {
        var tin = Tin.Create(request.Tin);

        var orgTask = organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == tin, cancellationToken);

        var whitelistTask = whitelistedRepository.Query()
            .AnyAsync(w => w.Tin == tin, cancellationToken);

        await Task.WhenAll(orgTask, whitelistTask);

        var organization = orgTask.Result;
        var isWhitelisted = whitelistTask.Result;

        if (organization == null)
            return !isWhitelisted;

        return organization.Status == OrganizationStatus.Trial && !isWhitelisted;
    }
}

public record GetOrganizationStatusQuery(string Tin) : IRequest<bool>;
