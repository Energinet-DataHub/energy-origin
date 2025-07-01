using System.Text.Json.Serialization;

namespace EnergyOrigin.Datahub3;

public class Token(string accessToken, int expiresIn)
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = accessToken;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = expiresIn;
}
