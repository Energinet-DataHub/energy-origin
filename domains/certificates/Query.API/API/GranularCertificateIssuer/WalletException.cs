using System;
using System.Runtime.Serialization;

namespace API.GranularCertificateIssuer;

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }

    protected WalletException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
