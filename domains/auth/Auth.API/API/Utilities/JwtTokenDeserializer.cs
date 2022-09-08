using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace API.Services;

public class JwtDeserializer : IJwtDeserializer
{
    public T DeserializeJwt<T>(string token)
    {
        var jwt = new JwtSecurityToken(token);
        var json = jwt.Payload.SerializeToJson();
        var info = JsonSerializer.Deserialize<T>(json);

        return info ?? throw new FormatException();
    }
}
