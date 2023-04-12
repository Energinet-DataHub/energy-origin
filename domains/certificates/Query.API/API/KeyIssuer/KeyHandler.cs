using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.KeyIssuer.Repositories;
using Marten.Exceptions;
using NSec.Cryptography;

namespace API.KeyIssuer;

internal class KeyHandler : IKeyIssuer
{
    private readonly SignatureAlgorithm algorithm;
    private readonly IKeyIssuingRepository repository;

    public KeyHandler(IKeyIssuingRepository repository)
    {
        algorithm = SignatureAlgorithm.Ed25519;
        this.repository = repository;
    }

    public async Task<string> Create(string meteringPointOwner, CancellationToken cancellationToken)
    {
        var document = await repository.GetByMeteringPointOwner(meteringPointOwner, cancellationToken);

        if (document != null)
        {
            return document.PublicKey;
        }

        using var key = Key.Create(algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving });
        var data = Encoding.UTF8.GetBytes(meteringPointOwner);
        var signature = algorithm.Sign(key, data);
        var encodedPrivateKey = Encode(key.Export(KeyBlobFormat.NSecPrivateKey));
        var encodedPublicKey = Encode(key.PublicKey.Export(KeyBlobFormat.NSecPublicKey));

        var keyDocument = new KeyIssuingDocument
        {
            MeteringPointOwner = meteringPointOwner,
            PublicKey = encodedPublicKey,
            PrivateKey = encodedPrivateKey,
            Signature = Encode(signature)
        };

        try
        {
            await repository.Save(keyDocument);
            return encodedPublicKey;
        }
        catch (DocumentAlreadyExistsException)
        {
            while (document == null)
            {
                document = await repository.GetByMeteringPointOwner(meteringPointOwner, cancellationToken);
            }

            return document.PublicKey;
        }
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
            throw new MartenException("Document does not exist");
        }

        var data = Encoding.UTF8.GetBytes(meteringPointOwner);
        var signature = Decode(keyDocument.Signature);

        return algorithm.Verify(decodedPublicKey, data, signature);
    }

    public string Encode(byte[] target) => Convert.ToBase64String(target);

    public byte[] Decode(string target) => Convert.FromBase64String(target);
}
