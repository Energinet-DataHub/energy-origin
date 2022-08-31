using System.Net.Http.Headers;
using EnergyOriginAuthorization;

namespace API.Extensions;

public static class HttpClientExtension
{
    public static HttpClient AddAuthorizationToken(this HttpClient client, AuthorizationContext context)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", context.Token);

        return client;
    }
}
