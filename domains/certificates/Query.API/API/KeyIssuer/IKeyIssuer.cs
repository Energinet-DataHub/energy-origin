using System.Threading;
using System.Threading.Tasks;

namespace API.KeyIssuer;

public interface IKeyIssuer
{
    public Task<string> Create(string meteringPointOwner, CancellationToken cancellationToken);
    public Task<bool> Verify(string publicKey, string meteringPointOwner, CancellationToken cancellationToken);
    public string Encode(byte[] target);
    public byte[] Decode(string target);
}

