using System.Net.Http.Headers;

namespace EnergyOriginAuthorization
{
    public static class HttpClientExtension
    {
        public static void AddAuthorizationToken(this HttpClient client, AuthorizationContext context)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", context.Token);
        }
    }
}
