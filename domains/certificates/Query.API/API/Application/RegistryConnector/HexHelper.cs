using System.Text;
using ProjectOrigin.Electricity.Client.Models;

namespace API.Application.RegistryConnector
{
    public static class HexHelper
    {
        public static string ToHex(CommandId commandId)
        {
            var bytes = commandId.Hash;
            var result = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
                result.Append(b.ToString("x2"));

            return result.ToString();
        }
    }
}
