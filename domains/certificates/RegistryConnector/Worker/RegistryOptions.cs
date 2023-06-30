using System;

namespace RegistryConnector.Worker;

public class RegistryOptions
{
    public const string Registry = nameof(Registry);

    public string Url { get; set; } = "";
    public byte[] IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
    public string RegistryName { get; set; } = "RegistryA";
}

public class ProjectOriginOptions
{
    public const string ProjectOrigin = nameof(ProjectOrigin);

    public string RegistryUrl { get; set; } = "";
    public string RegistryName { get; set; } = "RegistryA";
    public byte[] Dk1IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
    public byte[] Dk2IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();

    public string WalletUrl { get; set; } = "";
}
