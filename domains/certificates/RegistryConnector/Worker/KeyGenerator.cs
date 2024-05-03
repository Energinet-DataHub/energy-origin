using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using System;

namespace RegistryConnector.Worker;

public interface IKeyGenerator
{
    (IPublicKey, IPrivateKey) GenerateKeyInfo(long quantity, byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea);
}

public class KeyGenerator : IKeyGenerator
{
    private readonly PoRegistryOptions poRegistryOptions;

    public KeyGenerator(IOptions<PoRegistryOptions> projectOriginOptions)
    {
        this.poRegistryOptions = projectOriginOptions.Value;
    }

    public (IPublicKey, IPrivateKey) GenerateKeyInfo(long quantity, byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea)
    {
        if (quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {quantity} to uint");

        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive((int)walletDepositEndpointPosition).GetPublicKey();
        var issuerKey = poRegistryOptions.GetIssuerKey(gridArea);

        return (ownerPublicKey, issuerKey);
    }
}
