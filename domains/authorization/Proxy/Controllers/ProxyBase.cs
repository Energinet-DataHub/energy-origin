using Microsoft.AspNetCore.Mvc;

namespace Proxy.Controllers;

public class ProxyBase : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyBase(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private async Task ProxyRequest(string path, string organizationId)
    {
        var requestMessage = new HttpRequestMessage();
        var requestMethod = HttpContext.Request.Method;
        var requestContent = new StreamContent(HttpContext.Request.Body);

        requestMessage.Method = new HttpMethod(requestMethod);
        requestMessage.RequestUri = new Uri($"{path}{HttpContext.Request.QueryString}", UriKind.Relative);
        requestMessage.Content = requestContent;

        // Forward headers
        foreach (var header in HttpContext.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        requestMessage.Content?.Headers.TryAddWithoutValidation(WalletConstants.Header, organizationId);

        var client = _httpClientFactory.CreateClient("Proxy");
        var downstreamResponse = await client.SendAsync(requestMessage);

        foreach (var header in downstreamResponse.Headers.Where(x => !x.Key.Equals("Transfer-Encoding")))
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Add(header.Key, value);
            }
        }

        foreach (var header in downstreamResponse.Content.Headers)
        {
            foreach (var value in header.Value)
            {
                Response.Headers.Add(header.Key, value);
            }
        }

        Response.StatusCode = (int)downstreamResponse.StatusCode;

        await downstreamResponse.Content.CopyToAsync(Response.Body, null, default);

        Response.Body.Close();
    }

    protected async Task ProxyClientCredentialsRequest(string path, string organizationId)
    {
        // Security check first
        var orgIds = User.Claims.Where(x => x.Type == "org_ids").Select(x => x.Value).ToList();

        if (string.IsNullOrEmpty(organizationId) || !orgIds.Contains(organizationId))
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

}
