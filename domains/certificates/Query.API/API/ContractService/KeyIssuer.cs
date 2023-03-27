using System.Text;
using NSec.Cryptography;

namespace API.ContractService;

public class KeyIssuer
{
    public void Create(string meteringPointOwner)
    {
        // QUESTION: Do we need to store the private key, when we can verify through a message?
        var algorithm = SignatureAlgorithm.Ed25519;
        using var key = Key.Create(algorithm);

        var data = Encoding.UTF8.GetBytes(meteringPointOwner);

        var signature = algorithm.Sign(key, data);

        
    }

}
