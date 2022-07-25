using System.Text.Json.Serialization;

namespace API.Models;

public class LoginResponse
{
    [JsonPropertyName("login")]
    public IEnumerable<Login> Login { get; }

    public LoginResponse(IEnumerable<Login> login)
    {
        Login = login;
    }


}
