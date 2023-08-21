using System;
using System.Reflection;

namespace ProjectOriginClients;

internal static class WalletVersion
{
    /// <summary>
    /// Returns the version used by the protobuf file for the Wallet as defined in the .csproj file
    /// </summary>
    public static string Get() =>
        typeof(WalletVersion).Assembly
            .GetCustomAttribute<WalletVersionAttribute>()!
            .WalletVersion.Replace("\"", "");
}

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class WalletVersionAttribute : Attribute
{
    public string WalletVersion { get; }
    public WalletVersionAttribute(string walletVersion) => WalletVersion = walletVersion;
}
