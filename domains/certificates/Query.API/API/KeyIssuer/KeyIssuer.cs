using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.KeyIssuer.Repositories;
using NSec.Cryptography;

namespace API.KeyIssuer;

public class KeyIssuer : IKeyIssuer
{
    private readonly SignatureAlgorithm algorithm;
    private readonly IKeyIssuingRepository repository;

    public KeyIssuer(IKeyIssuingRepository repository)
    {
        algorithm = SignatureAlgorithm.Ed25519;
        this.repository = repository;
    }

    public async Task<string> Create(string meteringPointOwner, CancellationToken cancellationToken)
    {
        var generated = repository.GetByMeteringPointOwner(meteringPointOwner, cancellationToken);

        if (generated.Result != null)
        {
            return generated.Result.PublicKey;
        }

        using var key = Key.Create(algorithm);
        var data = Encoding.UTF8.GetBytes(meteringPointOwner);
        var signature = algorithm.Sign(key, data);

        var encodedPublicKey = Encode(key.PublicKey.Export(KeyBlobFormat.NSecPublicKey));

        var keyDocument = new KeyIssuingDocument
        {
            MeteringPointOwner = meteringPointOwner,
            PublicKey = encodedPublicKey,
            Signature = Encode(signature)
        };

        await repository.Save(keyDocument);

        return encodedPublicKey;
    }

    public async Task<bool> Verify(string publicKey, string meteringPointOwner, CancellationToken cancellationToken)
    {
        var decodedPublicKey = PublicKey.Import(
            algorithm,
            Decode(publicKey),
            KeyBlobFormat.NSecPublicKey
            );

        var keyDocument = await repository.GetByMeteringPointOwner(meteringPointOwner, cancellationToken);
        if (keyDocument == null)
        {
            throw new Exception("lolz");
        }

        var data = Encoding.UTF8.GetBytes(meteringPointOwner);
        var signature = Decode(keyDocument.Signature);

        return algorithm.Verify(decodedPublicKey, data, signature);
    }

    public string Encode(byte[] target) => Convert.ToBase64String(target);

    public byte[] Decode(string target) => Convert.FromBase64String(target);
}
