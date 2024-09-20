using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EnergyOrigin.TokenValidation.Utilities;

public class RsaKeyGenerator
{
    public static byte[] GenerateTestKey()
    {
        var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(csp.ExportParameters(true));
        var privateKeyPem = ExportPrivateKey(csp);
        return privateKeyPem;
    }

    private static byte[] ExportPrivateKey(RSACryptoServiceProvider csp)
    {
        if (csp.PublicOnly) throw new ArgumentException("CSP does not contain a private key", nameof(csp));
        var parameters = csp.ExportParameters(true);
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.Write((byte)0x30);
        using var innerStream = new MemoryStream();

        var innerWriter = new BinaryWriter(innerStream);
        EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 });
        EncodeIntegerBigEndian(innerWriter, parameters.Modulus!);
        EncodeIntegerBigEndian(innerWriter, parameters.Exponent!);
        EncodeIntegerBigEndian(innerWriter, parameters.D!);
        EncodeIntegerBigEndian(innerWriter, parameters.P!);
        EncodeIntegerBigEndian(innerWriter, parameters.Q!);
        EncodeIntegerBigEndian(innerWriter, parameters.DP!);
        EncodeIntegerBigEndian(innerWriter, parameters.DQ!);
        EncodeIntegerBigEndian(innerWriter, parameters.InverseQ!);
        var length = (int)innerStream.Length;
        EncodeLength(writer, length);
        writer.Write(innerStream.GetBuffer(), 0, length);


        var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");

        for (var i = 0; i < base64.Length; i += 64)
        {
            sb.AppendLine(new string(base64, i, Math.Min(64, base64.Length - i)));
        }
        sb.AppendLine("-----END RSA PRIVATE KEY-----");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void EncodeLength(BinaryWriter stream, int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative");
        if (length < 0x80)
        {
            stream.Write((byte)length);
        }
        else
        {
            var temp = length;
            var bytesRequired = 0;
            while (temp > 0)
            {
                temp >>= 8;
                bytesRequired++;
            }
            stream.Write((byte)(bytesRequired | 0x80));
            for (var i = bytesRequired - 1; i >= 0; i--)
            {
                stream.Write((byte)(length >> (8 * i) & 0xff));
            }
        }
    }

    private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
    {
        stream.Write((byte)0x02);
        var prefixZeros = value.TakeWhile(t => t == 0).Count();
        if (value.Length - prefixZeros == 0)
        {
            EncodeLength(stream, 1);
            stream.Write((byte)0);
        }
        else
        {
            if (forceUnsigned && value[prefixZeros] > 0x7f)
            {
                EncodeLength(stream, value.Length - prefixZeros + 1);
                stream.Write((byte)0);
            }
            else
            {
                EncodeLength(stream, value.Length - prefixZeros);
            }
            for (var i = prefixZeros; i < value.Length; i++)
            {
                stream.Write(value[i]);
            }
        }
    }
}
