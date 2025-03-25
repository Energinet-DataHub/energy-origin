using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public class AddOrganizationToWhitelistCommandHandler : IRequestHandler<AddOrganizationToWhitelistCommand, bool>
{
    private readonly IAuthorizationService _authorizationService;

    public AddOrganizationToWhitelistCommandHandler(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<bool> Handle(AddOrganizationToWhitelistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _authorizationService.AddOrganizationToWhitelistHttpRequestAsync(request.Tin);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class AddOrganizationToWhitelistCommand : IRequest<bool>
{
    public string Tin { get; init; } = string.Empty;
}
