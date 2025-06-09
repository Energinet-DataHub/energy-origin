using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetOrganizationStatusQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<GetOrganizationStatusQuery, bool>
{
    public async Task<bool> Handle(GetOrganizationStatusQuery request, CancellationToken cancellationToken)
    {
        var tin = Tin.Create(request.Tin);
        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(w => w.Tin == tin, cancellationToken);

        var status = organization?.Status;
        var loginType = request.LoginType.ToLowerInvariant();

        return (loginType, status) switch
        {
            ("normal", OrganizationStatus.Normal) => true,
            ("normal", null) => true,
            ("trial", OrganizationStatus.Trial) => true,
            ("trial", null) => true,
            _ => false
        };
    }
}

public record GetOrganizationStatusQuery(string Tin, string LoginType) : IRequest<bool>;
