using System;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using ProjectOrigin.WalletSystem.V1;

namespace API.Converters;

public class TempWalletDepositEndpoint
{
    public string Endpoint { get; set; }
    public byte[] PublicKey { get; set; }
    public int Version { get; set; }
}

public static class Base64Converter
{
    public static string ConvertObjectToBase64(object obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var base64String = Convert.ToBase64String(bytes);

        return base64String;
    }

    public static T? ConvertToObject<T>(string base64String)
    {
        var bytes = Convert.FromBase64String(base64String);
        var json = Encoding.UTF8.GetString(bytes);

        return JsonSerializer.Deserialize<T>(json);
    }

    public static string ConvertWalletDepositEndpointToBase64(WalletDepositEndpoint wde)
    {
        var temp = new TempWalletDepositEndpoint
        {
            Endpoint = wde.Endpoint,
            PublicKey = wde.PublicKey.ToByteArray(),
            Version = wde.Version
        };

        return ConvertObjectToBase64(temp);
    }

    public static WalletDepositEndpoint ConvertToWalletDepositEndpoint(string base64String)
    {
        var temp = ConvertToObject<TempWalletDepositEndpoint>(base64String);

        if (temp == null)
            throw new ArgumentException("Conversion failed.");

        return new WalletDepositEndpoint
        {
            Endpoint = temp.Endpoint,
            PublicKey = ByteString.CopyFrom(temp.PublicKey),
            Version = temp.Version
        };
    }
}
