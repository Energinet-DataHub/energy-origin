using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace RegistryConnector.Worker;

public class IssuerKey
{
    public Key Value { get; private set; }
    public IssuerKey(IOptions<RegistryOptions> registryOptions)
    {
        Value = Key.Import(SignatureAlgorithm.Ed25519, registryOptions.Value.IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKeyText);
    }
}
