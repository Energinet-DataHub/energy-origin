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
        var walletsResponse = await _walletClient.GetWalletsAsync(request.OrganizationId.Value, cancellationToken);

        var nonDisabledWallets = walletsResponse.Result.Where(x => x.DisabledDate == null).ToList();

        if (!nonDisabledWallets.Any())
            return;

        foreach (var wallet in nonDisabledWallets)
        {
            await _walletClient.DisableWalletAsync(wallet.Id, request.OrganizationId.Value, cancellationToken);
        }
    }
}

