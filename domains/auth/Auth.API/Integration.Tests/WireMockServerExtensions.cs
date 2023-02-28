using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Integration.Tests;

public static class WireMockServerExtensions
{
    public static WireMockServer MockConfigEndpoint(this WireMockServer server)
    {
        server.Given(
            Request.Create().WithPath("/op/.well-known/openid-configuration").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(
                File.ReadAllText("./openid-configuration.json").Replace("https://pp.netseidbroker.dk", $"http://localhost:{server.Port}")
            )
        );

        return server;
    }

    public static WireMockServer MockJwkEndpoint(this WireMockServer server, IdentityModel.Jwk.JsonWebKeySet? jwk = null)
    {
        var json = jwk == null ? File.ReadAllText("./jwks.json") : JsonSerializer.Serialize(jwk, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        server.Given(
            Request.Create().WithPath("/op/.well-known/openid-configuration/jwks").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(json)
        );

        return server;
    }

    public static WireMockServer MockTokenEndpoint(this WireMockServer server, string accessToken, string userToken, string identityToken)
    {
        server.Given(
            Request.Create().WithPath("/op/connect/token").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(
                JsonSerializer.Serialize(
                    new
                    {
                        access_token = accessToken,
                        userinfo_token = userToken,
                        id_token = identityToken
                    }))
        );

        return server;
    }
}
