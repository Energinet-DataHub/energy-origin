using System;

namespace API.RegistryConnector;

public class RegistryOptions
{
    public const string Registry = nameof(Registry);

    public string Url { get; set; } = string.Empty;

    public byte[] IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
}
