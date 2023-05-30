using System.Text;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker;

public static class CommandIdExtension
{
    public static string ToHex(this CommandId commandId)
    {
        var bytes = commandId.Hash;
        var result = new StringBuilder(bytes.Length * 2);

        foreach (var b in bytes)
            result.Append(b.ToString("x2"));

        return result.ToString();
    }
}
