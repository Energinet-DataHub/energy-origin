using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class GetOrganizationStatusQueryHandler(IOrganizationRepository organizationRepository)
    : IRequestHandler<GetOrganizationStatusQuery, LoginTypeValidationResult>
{
    public async Task<LoginTypeValidationResult> Handle(GetOrganizationStatusQuery request, CancellationToken cancellationToken)
    {
        var tin = Tin.Create(request.Tin);
        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(w => w.Tin == tin, cancellationToken);

        var status = organization?.Status;
        var loginType = request.LoginType.ToLowerInvariant();

        return (loginType, status) switch
        {
            ("normal", OrganizationStatus.Normal) => new LoginTypeValidationResult(true, OrganizationStatus.Normal),
            ("normal", null) => new LoginTypeValidationResult(true, null),
            ("trial", OrganizationStatus.Trial) => new LoginTypeValidationResult(true, OrganizationStatus.Trial),
            ("trial", null) => new LoginTypeValidationResult(true, null),
            _ => new LoginTypeValidationResult(false, status)
        };
    }
}

public record GetOrganizationStatusQuery(string Tin, string LoginType) : IRequest<LoginTypeValidationResult>;
public record LoginTypeValidationResult(bool IsValid, OrganizationStatus? OrgStatus);
