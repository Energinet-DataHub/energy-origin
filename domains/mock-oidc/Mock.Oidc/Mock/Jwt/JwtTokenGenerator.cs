using Jose;

namespace Mock.Oidc.Jwt;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly Jwk _jwk;


    public JwtTokenGenerator()
    {
        byte[] secretKey = { 71, 169, 0, 146, 29, 249, 85, 175, 83, 69, 213, 13, 6, 171, 190, 35, 10, 94, 255, 61, 57, 82, 91, 182, 100, 167, 217, 111, 54, 75, 193, 160 };
        _jwk = new Jwk(secretKey) { KeyId = "irrelevantKeyId" };
    }

    public string Generate(Dictionary<string, object> claims) => JWT.Encode(claims, _jwk, JwsAlgorithm.HS256);

    public IDictionary<string, object> GenerateJwk() => _jwk.ToDictionary();
}
