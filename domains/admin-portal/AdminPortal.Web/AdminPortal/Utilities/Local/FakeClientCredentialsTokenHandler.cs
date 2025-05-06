using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AdminPortal.Utilities.Local;

public class FakeClientCredentialsTokenHandler : DelegatingHandler
{
    private readonly string _fakeToken;

    public FakeClientCredentialsTokenHandler(string fakeToken = "fake-token-for-dev")
    {
        _fakeToken = fakeToken;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fakeToken);
        return base.SendAsync(request, cancellationToken);
    }
}
