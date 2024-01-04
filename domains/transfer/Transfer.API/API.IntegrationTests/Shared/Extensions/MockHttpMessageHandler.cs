using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace API.IntegrationTests.Shared.Extensions;

public class MockHttpMessageHandler(string response, HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(response)
        });
    }
}

