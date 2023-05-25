using System;
using System.Runtime.Serialization;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache;

[Serializable]
public class KeyAlreadyInCacheException : Exception
{
    public KeyAlreadyInCacheException(CommandId commandId) : base($"Key already exist in cache. CommandId: {HexHelper.ToHex(commandId)}") { }

    protected KeyAlreadyInCacheException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
