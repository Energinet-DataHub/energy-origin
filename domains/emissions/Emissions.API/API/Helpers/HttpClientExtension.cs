using System.Net.Http.Headers;
using EnergyOriginAuthorization;

namespace API.Helpers;

public static class HttpClientExtension
{
    public static HttpClient AddAuthorizationToken(this HttpClient client, AuthorizationContext authorizationContext)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationContext.Token);

        return client;
    }
}
