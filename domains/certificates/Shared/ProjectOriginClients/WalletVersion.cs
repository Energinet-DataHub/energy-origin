using System;
using System.Reflection;

namespace ProjectOriginClients;

internal static class WalletVersion
{
    public static string Get()
    {
        var assembly = typeof(WalletVersion).Assembly;
        var walletVersionAttribute = assembly.GetCustomAttribute<WalletVersionAttribute>();
        var walletVersion = walletVersionAttribute!.WalletVersion;
        return walletVersion.Replace("\"", "");
    }
}

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class WalletVersionAttribute : Attribute
{
    public string WalletVersion { get; }
    public WalletVersionAttribute(string walletVersion) => WalletVersion = walletVersion;
}
