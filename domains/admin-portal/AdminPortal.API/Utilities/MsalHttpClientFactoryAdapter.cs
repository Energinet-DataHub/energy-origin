using System.Net.Http;
using Microsoft.Identity.Client;

namespace AdminPortal.API.Utilities;

public class MsalHttpClientFactoryAdapter : IMsalHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MsalHttpClientFactoryAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient GetHttpClient()
    {
        return _httpClientFactory.CreateClient("Msal");
    }
}
