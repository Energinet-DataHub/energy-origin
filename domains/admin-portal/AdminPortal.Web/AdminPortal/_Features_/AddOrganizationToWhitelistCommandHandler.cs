using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace AdminPortal._Features_;

public class AddOrganizationToWhitelistCommand(Tin tin) : IRequest
{
    public Tin Tin { get; } = tin;
}

public class AddOrganizationToWhitelistCommandHandler : IRequestHandler<AddOrganizationToWhitelistCommand>
{
    private readonly IAuthorizationService _authorizationService;

    public AddOrganizationToWhitelistCommandHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task Handle(AddOrganizationToWhitelistCommand command, CancellationToken cancellationToken)
    {
        await _authorizationService.AddOrganizationToWhitelistAsync(command.Tin, cancellationToken);
    }
}
