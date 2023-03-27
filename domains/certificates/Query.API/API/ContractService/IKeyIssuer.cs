using NSec.Cryptography;

namespace API.ContractService;

public interface IKeyIssuer
{
    public (string PublicKey, string signature) Create(string meteringPointOwner);
    public bool Verify(PublicKey publicKey, byte[] data, byte[] signature);
    public string Encode(byte[] target);
    public byte[] Decode(string target);
}

