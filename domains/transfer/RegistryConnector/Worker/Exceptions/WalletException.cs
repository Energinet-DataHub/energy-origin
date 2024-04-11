using System;

namespace RegistryConnector.Worker.Exceptions;

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}
