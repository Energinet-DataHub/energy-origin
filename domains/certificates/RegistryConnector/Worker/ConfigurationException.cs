using System;
using System.Runtime.Serialization;

namespace RegistryConnector.Worker;

[Serializable]
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }

    protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
