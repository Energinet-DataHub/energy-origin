using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

public class ProxyBase : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ProxyBase(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Proxy");
    }

    private async Task ProxyRequest(string path, string organizationId)
    {
        var requestMessage = BuildProxyRequest(path);

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
        foreach (var header in proxyResponse.Headers.Where(x => !x.Key.Equals("Transfer-Encoding")))
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Add(header.Key, value);
            }
        }

        foreach (var header in proxyResponse.Content.Headers)
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Add(header.Key, value);
            }
        }
    }

    private async Task<HttpResponseMessage> RunProxyRequest(HttpRequestMessage requestMessage)
    {
        return await _httpClient.SendAsync(requestMessage);
    }

    private void BuildProxyForwardHeaders(string organizationId, HttpRequestMessage requestMessage)
    {
        foreach (var header in HttpContext.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Content?.Headers.TryAddWithoutValidation(WalletConstants.Header, organizationId);
    }

    private HttpRequestMessage BuildProxyRequest(string path)
    {
        var requestMessage = new HttpRequestMessage();
        var requestMethod = HttpContext.Request.Method;
        var requestContent = new StreamContent(HttpContext.Request.Body);

        requestMessage.Method = new HttpMethod(requestMethod);
        requestMessage.RequestUri = new Uri($"{path}{HttpContext.Request.QueryString}", UriKind.Relative);
        requestMessage.Content = requestContent;
        return requestMessage;
    }

    /// <summary>
    /// Proxies a request to the wallet service using the client credentials flow.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="organizationId"></param>
    protected async Task ProxyClientCredentialsRequest(string path, string? organizationId)
    {
        var orgIds = User.Claims.Where(x => x.Type == "org_ids").Select(x => x.Value).ToList();

        if (string.IsNullOrEmpty(organizationId) || !orgIds.Contains(organizationId) || Guid.TryParse(organizationId, out _))
        {
            Forbidden();
            return;
        }

        await ProxyRequest(path, organizationId);
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
        await ProxyRequest(path, "todo"); // TODO: This will most likely work, since we don't care about extra headers, but we shouldn't add it if not needed.
    }

}
