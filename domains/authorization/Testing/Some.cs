using System;
using System.Text;

namespace Testing;

public static class Some
{
    public const string Base64EncodedWalletDepositEndpoint = "eyJFbmRwb2ludCI6Imh0dHA6Ly9sb2NhbGhvc3Q6Nzg5MC8iLCJQdWJsaWNLZXkiOiJBVTBWVFVzQUFBQUJ5aE5KRmxENlZhVUZPajRGRzcybmVkSmxVbDRjK0xVejdpV0tRNEkzM1k0Q2J5OVBQTm5SdXRuaWUxT1NVRS9ud0RWTWV3bW14TnFFTkw5a0RZeHdMQWs9IiwiVmVyc2lvbiI6MX0=";

    public static string Gsrn()
    {
        var rand = new Random();
        var sb = new StringBuilder();
        sb.Append("57");
        for (var i = 0; i < 16; i++)
        {
            sb.Append(rand.Next(0, 9));
        }

        return sb.ToString();
    }

    public const string TechCode = "T070000";
    public const string FuelCode = "F00000000";
}
