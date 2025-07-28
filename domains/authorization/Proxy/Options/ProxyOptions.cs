using System;
using System.ComponentModel.DataAnnotations;

namespace Proxy.Options;

public class ProxyOptions
{
    public const string Prefix = "Proxy";

    [Required]
    public Uri WalletBaseUrl { get; set; } = null!;
}
