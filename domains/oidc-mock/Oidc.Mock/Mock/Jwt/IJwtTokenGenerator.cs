namespace Oidc.Mock.Jwt;

public interface IJwtTokenGenerator
{
    string Generate(Dictionary<string, object> claims);

    IDictionary<string, object> GetJwkProperties();
}
