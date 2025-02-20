using System.Net.Http;
using Microsoft.Identity.Client;
using RichardSzalay.MockHttp;

namespace AdminPortal.Tests;

public class FakeMsalHttpClientFactoryAdapter : IMsalHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public FakeMsalHttpClientFactoryAdapter()
    {
        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When("https://login.microsoftonline.com/*")
            .Respond("application/json",
                "{\"token_type\":\"Bearer\",\"expires_in\":3600,\"access_token\":\"fake-access-token\"}");

        _httpClient = mockHttp.ToHttpClient();
    }

    public HttpClient GetHttpClient() => _httpClient;
}
