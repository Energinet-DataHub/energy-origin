using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace AdminPortal._Features_;

public class RemoveOrganizationFromWhitelistCommand(Tin tin) : IRequest
{
    public Tin Tin { get; } = tin;
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
        await _authorizationService.AddOrganizationToWhitelistAsync(command.Tin, cancellationToken);
    }
}

