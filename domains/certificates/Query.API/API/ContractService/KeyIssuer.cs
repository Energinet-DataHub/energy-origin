using System;
using System.Text;
using NSec.Cryptography;

namespace API.ContractService;

public class KeyIssuer : IKeyIssuer
{
    private readonly SignatureAlgorithm algorithm;

    public KeyIssuer()
    {
        algorithm = SignatureAlgorithm.Ed25519;
    }

    public (string PublicKey, string signature) Create(string meteringPointOwner)
    {
        using var key = Key.Create(algorithm);

        var data = Encoding.UTF8.GetBytes(meteringPointOwner);

        var signature = algorithm.Sign(key, data);


        if (Verify(key.PublicKey, data, signature))
        {
            return (Encode(key.PublicKey.Export(KeyBlobFormat.NSecPublicKey)), Encode(signature));
        }

        throw new Exception("Key could not be generated");
    }

    public bool Verify(PublicKey publicKey, byte[] data, byte[] signature) => algorithm.Verify(publicKey, data, signature);

    public string Encode(byte[] target) => Convert.ToBase64String(target);

    public byte[] Decode(string target) => Convert.FromBase64String(target);
}
