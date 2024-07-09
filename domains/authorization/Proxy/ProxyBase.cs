using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Mvc;
using Proxy.Controllers;

namespace Proxy;

public class ProxyBase : ControllerBase
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly HttpClient _httpClient;

    public ProxyBase(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClientFactory.CreateClient("Proxy");
    }

    public ProxyBase(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Proxy");
    }

    private async Task ProxyRequest(string path, string? organizationId)
    {
        var requestMessage = await BuildProxyRequest(path);

        BuildProxyForwardHeaders(organizationId, requestMessage);

        var proxyResponse = await RunProxyRequest(requestMessage);

        BuildProxyResponseHeaders(proxyResponse);

        await BuildProxyResponse(proxyResponse);

        FlushRequest();
    }

    private void FlushRequest()
    {
        Response.Body.Close();
    }

    private async Task BuildProxyResponse(HttpResponseMessage proxyResponse)
    {
        Response.StatusCode = (int)proxyResponse.StatusCode;

        await proxyResponse.Content.CopyToAsync(Response.Body, null, default);
    }

    private void BuildProxyResponseHeaders(HttpResponseMessage proxyResponse)
    {
        foreach (var header in proxyResponse.Headers.Where(x => !x.Key.Equals("Transfer-Encoding", StringComparison.InvariantCultureIgnoreCase)))
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Append(header.Key, value);
            }
        }

        foreach (var header in proxyResponse.Content.Headers)
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Append(header.Key, value);
            }
        }
    }

    private async Task<HttpResponseMessage> RunProxyRequest(HttpRequestMessage requestMessage)
    {
        return await _httpClient.SendAsync(requestMessage);
    }

    private void BuildProxyForwardHeaders(string? organizationId, HttpRequestMessage requestMessage)
    {
        foreach (var header in HttpContext.Request.Headers.Where(x => !x.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase)))
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (organizationId is not null)
        {
            requestMessage.Content?.Headers.TryAddWithoutValidation(WalletConstants.Header, organizationId);
        }
    }

    private async Task<HttpRequestMessage> BuildProxyRequest(string path)
    {
        if (HttpContext.Request.Body.CanSeek)
        {
            HttpContext.Request.Body.Position = 0;
        }
        var requestBodyStream = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(requestBodyStream);
        requestBodyStream.Seek(0, SeekOrigin.Begin);

        var requestMessage = new HttpRequestMessage()
        {
            Content = new StreamContent(requestBodyStream)
        };

        requestMessage.Method = new HttpMethod(HttpContext.Request.Method);
        requestMessage.RequestUri = new Uri($"wallet-api/{path}{HttpContext.Request.QueryString}", UriKind.Relative);

        return requestMessage;
    }

    /// <summary>
    /// Proxies a request to the wallet service using the client credentials flow.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="organizationId"></param>
    protected async Task ProxyClientCredentialsRequest(string path, string? organizationId)
    {
        AccessDescriptor accessDescriptor = new(new IdentityDescriptor(_httpContextAccessor!));
        if (string.IsNullOrEmpty(organizationId))
        {
            Forbidden();
            return;
        }

        var orgId = Guid.Parse(organizationId);
        if (accessDescriptor is not null && !accessDescriptor.IsAuthorizedToOrganization(orgId))
        {
            Forbidden();
            return;
        }

        await ProxyRequest(path, organizationId!);
    }

    private void Forbidden()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.Body.Close();
    }

    /// <summary>
    /// Proxies a request to the wallet service using the internal token validation.
    /// </summary>
    /// <param name="path"></param>
    protected async Task ProxyTokenValidationRequest(string path)
    {
        var organizationId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(organizationId))
        {
            Forbidden();
            return;
        }

        await ProxyRequest(path, organizationId);
    }

    /// <summary>
    /// Proxies a request to the wallet service without any validation.
    /// </summary>
    /// <param name="path"></param>
    protected async Task ProxyInsecureCall(string path)
    {
        await ProxyRequest(path, null);
    }

}
