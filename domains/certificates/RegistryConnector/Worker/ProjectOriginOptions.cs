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

    public IPrivateKey Dk1IssuerKey => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(Dk1IssuerPrivateKeyPem));
    public IPrivateKey Dk2IssuerKey => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(Dk2IssuerPrivateKeyPem));
}
