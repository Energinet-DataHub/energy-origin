using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public class JwkTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public JwkTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ReturnsJwks()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var jwkResponse = await client.GetAsync("/.well-known/openid-configuration/jwks");
        Assert.Equal(HttpStatusCode.OK, jwkResponse.StatusCode);

        //_testOutputHelper.WriteLine(await jwkResponse.Content.ReadAsStringAsync());
    }
}