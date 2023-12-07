using System;

namespace API.GranularCertificateIssuer;

public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}
