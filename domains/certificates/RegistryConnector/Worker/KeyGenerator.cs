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
    private readonly ProjectOriginRegistryOptions projectOriginRegistryOptions;

    public KeyGenerator(IOptions<ProjectOriginRegistryOptions> projectOriginOptions)
    {
        this.projectOriginRegistryOptions = projectOriginOptions.Value;
    }

    public (IPublicKey, IPrivateKey) GenerateKeyInfo(long quantity, byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea)
    {
        if (quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {quantity} to uint");

        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive((int)walletDepositEndpointPosition).GetPublicKey();
        var issuerKey = projectOriginRegistryOptions.GetIssuerKey(gridArea);

        return (ownerPublicKey, issuerKey);
    }
}
