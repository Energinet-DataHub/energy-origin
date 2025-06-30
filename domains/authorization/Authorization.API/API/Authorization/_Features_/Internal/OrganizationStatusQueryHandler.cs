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

        var orgStatus = organization?.Status;
        var clientLoginType = request.LoginType.ToLowerInvariant();

        return (loginType: clientLoginType, status: orgStatus) switch
        {
            ("normal", OrganizationStatus.Deactivated) => new LoginTypeValidationResult(IsAllowedAccess: true, GrantedAccessAsTypeOf: OrganizationStatus.Deactivated),
            ("normal", OrganizationStatus.Normal) => new LoginTypeValidationResult(IsAllowedAccess: true, GrantedAccessAsTypeOf: OrganizationStatus.Normal),
            ("normal", null) => new LoginTypeValidationResult(IsAllowedAccess: true, GrantedAccessAsTypeOf: OrganizationStatus.Normal),
            ("trial", OrganizationStatus.Trial) => new LoginTypeValidationResult(IsAllowedAccess: true, GrantedAccessAsTypeOf: OrganizationStatus.Trial),
            ("trial", null) => new LoginTypeValidationResult(IsAllowedAccess: true, GrantedAccessAsTypeOf: OrganizationStatus.Trial),
            _ => new LoginTypeValidationResult(IsAllowedAccess: false, orgStatus)
        };
    }
}

public record GetOrganizationStatusQuery(string Tin, string LoginType) : IRequest<LoginTypeValidationResult>;
public record LoginTypeValidationResult(bool IsAllowedAccess, OrganizationStatus? GrantedAccessAsTypeOf);
