using System;

namespace API.Authorization.Controllers;

public static class ClientTypeMapper
{
    public static API.Models.ClientType MapToDatabaseClientType(ClientType apiClientType)
    {
        switch (apiClientType)
        {
            case ClientType.External:
                return API.Models.ClientType.External;
            case ClientType.Internal:
                return API.Models.ClientType.Internal;
            default:
                throw new ArgumentOutOfRangeException(nameof(apiClientType), apiClientType, null);
        }
    }

    public static ClientType MapToApiClientType(Models.ClientType databaseClientType)
    {
        switch (databaseClientType)
        {
            case API.Models.ClientType.External:
                return ClientType.External;
            case API.Models.ClientType.Internal:
                return ClientType.Internal;
            default:
                throw new ArgumentOutOfRangeException(nameof(databaseClientType), databaseClientType, null);
        }
    }
}
