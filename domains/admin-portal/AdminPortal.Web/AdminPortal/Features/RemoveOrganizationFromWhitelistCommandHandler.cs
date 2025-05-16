using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace AdminPortal.Features;

public class RemoveOrganizationFromWhitelistCommand(string tin) : IRequest
{
    public Tin Tin { get; } = Tin.Create(tin);
}

public class RemoveOrganizationFromWhitelistCommandHandler : IRequestHandler<RemoveOrganizationFromWhitelistCommand>
{
    private readonly IAuthorizationService _authorizationService;

    public RemoveOrganizationFromWhitelistCommandHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task Handle(RemoveOrganizationFromWhitelistCommand command, CancellationToken cancellationToken)
    {
        await _authorizationService.RemoveOrganizationFromWhitelistAsync(command.Tin, cancellationToken);
    }
}

