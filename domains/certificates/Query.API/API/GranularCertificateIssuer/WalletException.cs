using System;

namespace API.GranularCertificateIssuer;

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}
