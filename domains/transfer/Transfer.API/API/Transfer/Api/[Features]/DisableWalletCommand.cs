using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using MediatR;

namespace API.Transfer.Api._Features_;

public record DisableWalletCommand(OrganizationId OrganizationId) : IRequest;

public class DisableWalletCommandCommandHandler : IRequestHandler<DisableWalletCommand>
{
    private readonly IWalletClient _walletClient;

    public DisableWalletCommandCommandHandler(IWalletClient walletClient)
    {
        _walletClient = walletClient;
    }

    public async Task Handle(DisableWalletCommand request, CancellationToken cancellationToken)
    {
        var walletsResponse = await _walletClient.GetWallets(request.OrganizationId.Value, cancellationToken);

        if (!walletsResponse.Result.Any())
            return;


    }
}

