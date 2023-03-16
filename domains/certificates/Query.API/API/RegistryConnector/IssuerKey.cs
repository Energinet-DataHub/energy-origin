using Google.Protobuf;
using NSec.Cryptography;

namespace API.RegistryConnector;

public static class IssuerKey
{
    public const string PrivateKey = "4jqufvs4Q0/r7/dEbxk/4dmrOkQZ1iY0ilqWmrFEcZs=";
    public const string PublicKey = "sJuwdQ4TOarGjc3CyEJ5c37jJNOaH6PcOiyE1ge6+24=";

    public static Key LoadPrivateKey()
    {
        var byteString = ByteString.FromBase64(PrivateKey);
        return Key.Import(SignatureAlgorithm.Ed25519, byteString.Span, KeyBlobFormat.RawPrivateKey);
    }
}
