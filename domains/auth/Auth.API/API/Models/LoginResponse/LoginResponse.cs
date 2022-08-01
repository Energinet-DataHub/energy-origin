using System.Text.Json.Serialization;

namespace API.Models;

public class LoginResponse
{
    [JsonPropertyName("next_url")]
    public string NextUrl { get; set; }

    public LoginResponse(string nextUrl)
    {
        NextUrl = nextUrl;
    }


}
