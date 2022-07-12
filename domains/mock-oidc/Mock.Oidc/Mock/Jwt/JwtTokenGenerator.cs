namespace Mock.Oidc.Jwt;

using JWT.Algorithms;
using JWT.Builder;
using Mock.Oidc.Controllers;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    public string Generate(Dictionary<string, object> claims)
    {
        var rsa = RSAProvider.RSA;

        return JwtBuilder.Create()
            .WithAlgorithm(new RS256Algorithm(rsa, rsa))
            .AddClaims(claims)
            .Encode();
    }
}
