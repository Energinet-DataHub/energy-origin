using System;

namespace API.Authorization.Controllers;

public static class ClientTypeMapper
{
    public static API.Models.ClientType MapToDatabaseClientType(ClientType sourceClientType)
    {
        switch (sourceClientType)
        {
            case ClientType.External:
                return API.Models.ClientType.External;
            case ClientType.Internal:
                return API.Models.ClientType.Internal;
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceClientType), sourceClientType, null);
        }
    }
}
