using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Proxy.Controllers;

namespace Proxy;

public class ProxyBase : ControllerBase
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private static readonly string[] SkipForward = ["Authorization", "Transfer-Encoding"];

    public ProxyBase(IHttpClientFactory httpClientFactory, IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClientFactory.CreateClient("Proxy");
    }

    public ProxyBase(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Proxy");
    }

    private async Task ProxyRequest(string path, string? organizationId, string? customQueryString = null)
    {
        using var requestMessage = await BuildProxyRequest(path, customQueryString);
        BuildProxyForwardHeaders(organizationId, requestMessage);

        using var proxyResponse = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        BuildProxyResponseHeaders(proxyResponse);
        Response.StatusCode = (int)proxyResponse.StatusCode;
        await proxyResponse.Content.CopyToAsync(Response.Body);
    }

    private void BuildProxyResponseHeaders(HttpResponseMessage proxyResponse)
    {
        foreach (var header in proxyResponse.Headers.Concat(proxyResponse.Content.Headers))
            foreach (var value in header.Value)
                if (!SkipForward.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                    Response.Headers.Append(header.Key, value);
    }

    private void BuildProxyForwardHeaders(string? organizationId, HttpRequestMessage request)
    {
        foreach (var h in HttpContext.Request.Headers.Where(h => !SkipForward.Contains(h.Key, StringComparer.OrdinalIgnoreCase)))
            if (!request.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray()))
                request.Content?.Headers.TryAddWithoutValidation(h.Key, h.Value.ToArray());

        if (organizationId is not null)
            request.Content?.Headers.TryAddWithoutValidation(WalletConstants.Header, organizationId);
    }

    private async Task<HttpRequestMessage> BuildProxyRequest(string path, string? customQueryString = null)
    {
        if (HttpContext.Request.Body.CanSeek)
            HttpContext.Request.Body.Position = 0;

        var buffer = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(buffer);
        buffer.Position = 0;

        var queryString = customQueryString ?? HttpContext.Request.QueryString.ToString();
        var req = new HttpRequestMessage
        {
            Method = new HttpMethod(HttpContext.Request.Method),
            Content = new StreamContent(buffer),
            RequestUri = new Uri($"wallet-api/{path}{queryString}", UriKind.Relative)
        };
        return req;
    }

    /// <summary>
    /// Proxies a request to the wallet service using the client credentials flow.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="organizationId"></param>
    /// <param name="customQueryString"></param>
    protected async Task<IActionResult> ProxyClientCredentialsRequest(string path, string? organizationId, string? customQueryString = null)
    {
        if (_httpContextAccessor is null)
            return Forbidden();

        if (!Guid.TryParse(organizationId, out var orgGuid))
            return BadRequest("Query‑parameter 'organizationId' must be a non‑empty GUID.");

        var accessDescriptor = new AccessDescriptor(new IdentityDescriptor(_httpContextAccessor));

        if (!accessDescriptor.IsAuthorizedToOrganization(orgGuid))
            return Forbidden();

        await ProxyRequest(path, organizationId, customQueryString);

        return new EmptyResult();
    }

    /// <summary>
    /// Proxies a request to the wallet service without any validation.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="customQueryString"></param>
    protected async Task<IActionResult> ProxyInsecureCall(string path, string? customQueryString = null)
    {
        await ProxyRequest(path, null, customQueryString);
        return new EmptyResult();
    }

    private ObjectResult Forbidden(string? detail = null)
    {
        return Problem(detail, statusCode: StatusCodes.Status403Forbidden);
    }

    private ObjectResult BadRequest(string detail)
    {
        return Problem(detail, statusCode: StatusCodes.Status400BadRequest);
    }
}
