using System;
using System.Text;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace RegistryConnector.Worker;

//TODO: Add validation
public class ProjectOriginOptions
{
    public const string ProjectOrigin = nameof(ProjectOrigin);

    public string RegistryUrl { get; set; } = "";
    public string RegistryName { get; set; } = "RegistryA";
    public byte[] Dk1IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
    public byte[] Dk2IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();

    public string WalletUrl { get; set; } = "";

    public IPrivateKey GetIssuerKey(string gridArea)
    {
        if (gridArea.Equals("DK1", StringComparison.OrdinalIgnoreCase))
            return ToPrivateKey(Dk1IssuerPrivateKeyPem);

        if (gridArea.Equals("DK2", StringComparison.OrdinalIgnoreCase))
            return ToPrivateKey(Dk2IssuerPrivateKeyPem);

        throw new Exception($"Not supported GridArea {gridArea}"); //TODO: How to handle this
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}
