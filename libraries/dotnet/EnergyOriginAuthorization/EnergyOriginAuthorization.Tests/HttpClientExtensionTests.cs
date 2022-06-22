using System.Net.Http;
using EnergyOriginAuthorization;
using Xunit;

namespace Tests;

public sealed class HttpClientExtensionTests
{
    [Fact]
    public void AddAuthorizationTokenTest_valid_token()
    {
        var client = new HttpClient();
        var context = new AuthorizationContext("subject", "actor", "token");

        client.AddAuthorizationToken(context);

        Assert.NotNull(client);
        Assert.NotEmpty(client.DefaultRequestHeaders);
        Assert.Equal(context.Token, client.DefaultRequestHeaders.Authorization?.Parameter ?? "");
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization?.Scheme ?? "");
    }
}
