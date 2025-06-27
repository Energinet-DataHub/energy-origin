using System.Text.Json.Serialization;

namespace EnergyOrigin.Datahub3;

public class Token(string accessToken, long expires)
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = accessToken;
    [JsonPropertyName("expires_on")]
    public long Expires { get; set; } = expires;
}
