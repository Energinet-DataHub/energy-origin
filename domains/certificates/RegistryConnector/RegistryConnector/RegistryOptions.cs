using System;

namespace RegistryConnector;

public class RegistryOptions
{
    public const string Registry = nameof(Registry);

    public string Url { get; set; } = "";
    public byte[] IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
}
